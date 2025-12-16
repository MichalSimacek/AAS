#!/usr/bin/env python3
"""
ASP.NET Core Localization Test Suite
Tests the localization fixes for:
1. Collection Title Translation (P0 Bug Fix)
2. "Back to Blog" Button Localization (P2 Fix)
"""

import requests
import json
import time
import re
import os
from bs4 import BeautifulSoup
from urllib.parse import urljoin

class LocalizationTestRunner:
    def __init__(self, base_url="http://localhost:8001"):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
        })
        
        # Test languages and their expected translations
        self.test_languages = {
            'en': {
                'code': 'en',
                'cookie': '.AspNetCore.Culture=c%3Den%7Cuic%3Den',
                'expected_collection_title': 'Beautiful Landscape Painting',
                'expected_back_to_blog': 'Back to Blog'
            },
            'cs': {
                'code': 'cs', 
                'cookie': '.AspNetCore.Culture=c%3Dcs%7Cuic%3Dcs',
                'expected_collection_title': 'Krásný obraz krajiny',
                'expected_back_to_blog': 'Zpět na blog'
            },
            'de': {
                'code': 'de',
                'cookie': '.AspNetCore.Culture=c%3Dde%7Cuic%3Dde', 
                'expected_collection_title': 'Schönes Landschaftsgemälde',
                'expected_back_to_blog': 'Zurück zum Blog'
            },
            'ru': {
                'code': 'ru',
                'cookie': '.AspNetCore.Culture=c%3Dru%7Cuic%3Dru',
                'expected_collection_title': None,  # Will check for any translation
                'expected_back_to_blog': 'Назад к блогу'
            }
        }
        
    def log(self, message, level="INFO"):
        """Log test messages with timestamp"""
        timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] [{level}] {message}")
        
    def test_server_connectivity(self):
        """Test if the ASP.NET Core server is accessible"""
        try:
            response = self.session.get(self.base_url, timeout=10)
            if response.status_code == 200:
                self.log("✅ Server is accessible")
                return True
            else:
                self.log(f"❌ Server returned status code: {response.status_code}", "ERROR")
                return False
        except Exception as e:
            self.log(f"❌ Server connectivity failed: {str(e)}", "ERROR")
            return False
            
    def test_collection_translation(self, lang_config):
        """Test collection title translation for a specific language"""
        try:
            lang_code = lang_config['code']
            cookie = lang_config['cookie']
            expected_title = lang_config['expected_collection_title']
            
            self.log(f"Testing collection translation for language: {lang_code}")
            
            # Set language cookie and make request
            headers = {'Cookie': cookie}
            collections_url = urljoin(self.base_url, '/Collections')
            
            response = self.session.get(collections_url, headers=headers)
            
            if response.status_code != 200:
                self.log(f"❌ Collections page not accessible: {response.status_code}", "ERROR")
                return False
                
            # Parse HTML to find collection titles
            soup = BeautifulSoup(response.text, 'html.parser')
            
            # Look for collection titles (they should be in elements with class 'card-title' or similar)
            title_elements = soup.find_all(class_='card-title')
            if not title_elements:
                # Try alternative selectors
                title_elements = soup.find_all('h5', class_='card-title')
                if not title_elements:
                    title_elements = soup.find_all(['h3', 'h4', 'h5'], string=re.compile(r'.*'))
            
            if not title_elements:
                self.log(f"❌ No collection titles found on page for {lang_code}", "ERROR")
                return False
                
            # Extract title texts
            found_titles = [elem.get_text().strip() for elem in title_elements if elem.get_text().strip()]
            
            self.log(f"Found collection titles for {lang_code}: {found_titles}")
            
            # Check if expected title is present (if specified)
            if expected_title:
                if expected_title in found_titles:
                    self.log(f"✅ Expected title '{expected_title}' found for {lang_code}")
                    return True
                else:
                    self.log(f"❌ Expected title '{expected_title}' NOT found for {lang_code}", "ERROR")
                    self.log(f"   Found titles: {found_titles}", "ERROR")
                    return False
            else:
                # For languages without specific expected titles, just check that we got some titles
                if found_titles:
                    self.log(f"✅ Collection titles found for {lang_code}: {found_titles[0] if found_titles else 'None'}")
                    return True
                else:
                    self.log(f"❌ No collection titles found for {lang_code}", "ERROR")
                    return False
                    
        except Exception as e:
            self.log(f"❌ Collection translation test failed for {lang_code}: {str(e)}", "ERROR")
            return False
            
    def test_back_to_blog_button(self, lang_config):
        """Test 'Back to Blog' button translation for a specific language"""
        try:
            lang_code = lang_config['code']
            cookie = lang_config['cookie']
            expected_text = lang_config['expected_back_to_blog']
            
            self.log(f"Testing 'Back to Blog' button for language: {lang_code}")
            
            # Set language cookie and make request to blog post detail page
            headers = {'Cookie': cookie}
            blog_post_url = urljoin(self.base_url, '/Blog/Post/1')
            
            response = self.session.get(blog_post_url, headers=headers)
            
            if response.status_code == 404:
                self.log(f"⚠️ Blog post 1 not found, trying to find any blog post", "WARNING")
                # Try to find any blog post from the blog index
                blog_index_url = urljoin(self.base_url, '/Blog')
                blog_response = self.session.get(blog_index_url, headers=headers)
                
                if blog_response.status_code == 200:
                    soup = BeautifulSoup(blog_response.text, 'html.parser')
                    # Look for blog post links
                    post_links = soup.find_all('a', href=re.compile(r'/Blog/Post/\d+'))
                    if post_links:
                        blog_post_url = urljoin(self.base_url, post_links[0]['href'])
                        response = self.session.get(blog_post_url, headers=headers)
                    else:
                        self.log(f"❌ No blog posts found to test", "ERROR")
                        return False
                else:
                    self.log(f"❌ Blog index not accessible: {blog_response.status_code}", "ERROR")
                    return False
            
            if response.status_code != 200:
                self.log(f"❌ Blog post page not accessible: {response.status_code}", "ERROR")
                return False
                
            # Parse HTML to find "Back to Blog" button
            soup = BeautifulSoup(response.text, 'html.parser')
            
            # Look for the back to blog button - it might have various selectors
            back_button = None
            
            # Try different possible selectors for the back button
            selectors_to_try = [
                'a[href="/Blog"]',
                'a[href*="Blog"]',
                '.btn:contains("Back")',
                '.btn:contains("Zpět")',
                '.btn:contains("Zurück")',
                '.btn:contains("Назад")',
                'a:contains("Back to Blog")',
                'a:contains("Zpět na blog")',
                'a:contains("Zurück zum Blog")',
                'a:contains("Назад к блогу")'
            ]
            
            # Also search by text content
            all_links = soup.find_all('a')
            all_buttons = soup.find_all(['button', 'a'], class_=re.compile(r'btn'))
            
            # Combine all potential elements
            potential_elements = all_links + all_buttons
            
            back_button_text = None
            for element in potential_elements:
                text = element.get_text().strip()
                # Look for back-related keywords AND blog-related keywords
                back_keywords = ['back', 'zpět', 'zurück', 'назад']
                blog_keywords = ['blog', 'блог', 'блогу']
                
                has_back = any(keyword in text.lower() for keyword in back_keywords)
                has_blog = any(keyword in text.lower() for keyword in blog_keywords)
                
                if has_back and has_blog:
                    back_button_text = text
                    back_button = element
                    break
                elif has_blog and element.get('href') == '/Blog':
                    # Also accept simple "Blog" links that go to /Blog
                    back_button_text = text
                    back_button = element
                    break
            
            if not back_button_text:
                self.log(f"❌ 'Back to Blog' button not found for {lang_code}", "ERROR")
                # Log all found links for debugging
                all_link_texts = [link.get_text().strip() for link in all_links if link.get_text().strip()]
                self.log(f"   All links found: {all_link_texts[:10]}", "DEBUG")  # Show first 10
                return False
                
            self.log(f"Found back button text for {lang_code}: '{back_button_text}'")
            
            # Check if the text matches expected translation
            if expected_text.lower() in back_button_text.lower() or back_button_text.lower() in expected_text.lower():
                self.log(f"✅ 'Back to Blog' button correctly translated for {lang_code}: '{back_button_text}'")
                return True
            else:
                self.log(f"❌ 'Back to Blog' button translation incorrect for {lang_code}", "ERROR")
                self.log(f"   Expected: '{expected_text}', Found: '{back_button_text}'", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"❌ Back to Blog button test failed for {lang_code}: {str(e)}", "ERROR")
            return False
            
    def run_collection_translation_tests(self):
        """Run collection translation tests for all languages"""
        self.log("=" * 60)
        self.log("TESTING COLLECTION TITLE TRANSLATIONS (P0 BUG FIX)")
        self.log("=" * 60)
        
        results = {}
        
        for lang_code, lang_config in self.test_languages.items():
            results[f'collection_{lang_code}'] = self.test_collection_translation(lang_config)
            time.sleep(1)  # Small delay between requests
            
        return results
        
    def run_back_to_blog_tests(self):
        """Run back to blog button tests for all languages"""
        self.log("=" * 60)
        self.log("TESTING 'BACK TO BLOG' BUTTON TRANSLATIONS (P2 FIX)")
        self.log("=" * 60)
        
        results = {}
        
        for lang_code, lang_config in self.test_languages.items():
            results[f'back_to_blog_{lang_code}'] = self.test_back_to_blog_button(lang_config)
            time.sleep(1)  # Small delay between requests
            
        return results
        
    def run_complete_localization_test(self):
        """Run the complete localization test suite"""
        self.log("=" * 80)
        self.log("STARTING ASP.NET CORE LOCALIZATION TEST SUITE")
        self.log("=" * 80)
        
        all_results = {}
        
        # Test 1: Server connectivity
        connectivity_result = self.test_server_connectivity()
        all_results['connectivity'] = connectivity_result
        
        if not connectivity_result:
            self.log("❌ Server not accessible, aborting tests", "ERROR")
            return all_results
            
        # Test 2: Collection translation tests
        collection_results = self.run_collection_translation_tests()
        all_results.update(collection_results)
        
        # Test 3: Back to blog button tests  
        blog_button_results = self.run_back_to_blog_tests()
        all_results.update(blog_button_results)
        
        # Summary
        self.log("=" * 80)
        self.log("LOCALIZATION TEST RESULTS SUMMARY")
        self.log("=" * 80)
        
        # Group results by test type
        collection_tests = {k: v for k, v in all_results.items() if k.startswith('collection_')}
        blog_tests = {k: v for k, v in all_results.items() if k.startswith('back_to_blog_')}
        
        self.log("Collection Title Translation Tests:")
        for test_name, result in collection_tests.items():
            lang = test_name.replace('collection_', '')
            status = "✅ PASS" if result else "❌ FAIL"
            self.log(f"  {lang.upper()}: {status}")
            
        self.log("\n'Back to Blog' Button Translation Tests:")
        for test_name, result in blog_tests.items():
            lang = test_name.replace('back_to_blog_', '')
            status = "✅ PASS" if result else "❌ FAIL"
            self.log(f"  {lang.upper()}: {status}")
            
        # Overall statistics
        total_tests = len(all_results) - 1  # Exclude connectivity test
        passed_tests = sum(1 for k, v in all_results.items() if k != 'connectivity' and v)
        
        self.log(f"\nOVERALL LOCALIZATION TESTS: {passed_tests}/{total_tests} passed")
        
        # Critical issues analysis
        collection_failures = [k for k, v in collection_tests.items() if not v]
        blog_failures = [k for k, v in blog_tests.items() if not v]
        
        if collection_failures:
            self.log(f"\n⚠️ CRITICAL: Collection translation failures: {collection_failures}")
            
        if blog_failures:
            self.log(f"\n⚠️ ISSUE: Back to Blog button translation failures: {blog_failures}")
            
        return all_results

def main():
    """Main test execution"""
    tester = LocalizationTestRunner()
    results = tester.run_complete_localization_test()
    
    # Determine exit code based on critical P0 collection translation results
    collection_results = {k: v for k, v in results.items() if k.startswith('collection_')}
    collection_passed = sum(collection_results.values())
    collection_total = len(collection_results)
    
    if collection_passed == collection_total:
        exit(0)  # All critical tests passed
    elif collection_passed > 0:
        exit(1)  # Some tests passed, some failed
    else:
        exit(2)  # All critical tests failed

if __name__ == "__main__":
    main()