#!/usr/bin/env python3
"""
ASP.NET Core Blog Post Creation Flow Test
Tests the complete blog post creation workflow including:
1. Admin login
2. Blog post creation form
3. Form submission
4. Verification of post creation
5. Log analysis for diagnostic messages
"""

import requests
import json
import time
import re
import os
from bs4 import BeautifulSoup
from urllib.parse import urljoin

class BlogTestRunner:
    def __init__(self, base_url="http://localhost:8001"):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
        })
        self.admin_credentials = {
            'email': 'admin@localhost',
            'password': 'Admin123!@#$'
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
            
    def get_antiforgery_token(self, html_content):
        """Extract antiforgery token from HTML"""
        soup = BeautifulSoup(html_content, 'html.parser')
        token_input = soup.find('input', {'name': '__RequestVerificationToken'})
        if token_input:
            return token_input.get('value')
        return None
        
    def test_admin_login(self):
        """Test admin login functionality"""
        try:
            # Get login page
            login_url = urljoin(self.base_url, '/Identity/Account/Login')
            self.log(f"Accessing login page: {login_url}")
            
            response = self.session.get(login_url)
            if response.status_code != 200:
                self.log(f"❌ Login page not accessible: {response.status_code}", "ERROR")
                return False
                
            # Extract antiforgery token
            token = self.get_antiforgery_token(response.text)
            if not token:
                self.log("❌ Could not find antiforgery token on login page", "ERROR")
                return False
                
            # Submit login form
            login_data = {
                'Email': self.admin_credentials['email'],
                'Password': self.admin_credentials['password'],
                'RememberMe': 'false',
                '__RequestVerificationToken': token
            }
            
            self.log(f"Attempting login with email: {self.admin_credentials['email']}")
            response = self.session.post(login_url, data=login_data, allow_redirects=False)
            
            # Check for successful login (redirect)
            if response.status_code in [302, 303]:
                self.log("✅ Admin login successful")
                return True
            else:
                self.log(f"❌ Login failed with status: {response.status_code}", "ERROR")
                if "Invalid login attempt" in response.text:
                    self.log("❌ Invalid credentials", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"❌ Login test failed: {str(e)}", "ERROR")
            return False
            
    def test_blog_create_page_access(self):
        """Test access to blog creation page"""
        try:
            create_url = urljoin(self.base_url, '/Admin/Blog/Create')
            self.log(f"Accessing blog create page: {create_url}")
            
            response = self.session.get(create_url)
            if response.status_code == 200:
                self.log("✅ Blog create page accessible")
                
                # Check for TinyMCE editor
                if 'tinymce' in response.text.lower():
                    self.log("✅ TinyMCE editor detected on page")
                else:
                    self.log("⚠️ TinyMCE editor not detected", "WARNING")
                    
                # Check for form fields
                soup = BeautifulSoup(response.text, 'html.parser')
                title_field = soup.find('input', {'name': 'TitleCs'})
                content_field = soup.find('textarea', {'name': 'ContentCs'})
                published_field = soup.find('input', {'name': 'Published'})
                
                if title_field and content_field:
                    self.log("✅ Required form fields found")
                else:
                    self.log("❌ Missing required form fields", "ERROR")
                    return False, None
                    
                return True, response.text
            else:
                self.log(f"❌ Blog create page not accessible: {response.status_code}", "ERROR")
                return False, None
                
        except Exception as e:
            self.log(f"❌ Blog create page test failed: {str(e)}", "ERROR")
            return False, None
            
    def test_blog_post_creation(self):
        """Test blog post creation with form submission"""
        try:
            # Get create page first
            success, page_content = self.test_blog_create_page_access()
            if not success:
                return False
                
            # Extract antiforgery token
            token = self.get_antiforgery_token(page_content)
            if not token:
                self.log("❌ Could not find antiforgery token on create page", "ERROR")
                return False
                
            # Prepare test blog post data
            test_data = {
                'TitleCs': 'Test Blog Post',
                'ContentCs': 'This is test content for the blog post',
                'Published': 'true',
                '__RequestVerificationToken': token
            }
            
            self.log("Submitting blog post creation form...")
            self.log(f"Title: {test_data['TitleCs']}")
            self.log(f"Content: {test_data['ContentCs']}")
            self.log(f"Published: {test_data['Published']}")
            
            # Submit form
            create_url = urljoin(self.base_url, '/Admin/Blog/Create')
            response = self.session.post(create_url, data=test_data, allow_redirects=False)
            
            # Check response
            if response.status_code in [302, 303]:
                redirect_location = response.headers.get('Location', '')
                if '/Admin/Blog' in redirect_location:
                    self.log("✅ Blog post creation form submitted successfully")
                    return True
                else:
                    self.log(f"⚠️ Unexpected redirect location: {redirect_location}", "WARNING")
                    return True
            elif response.status_code == 200:
                # Check for validation errors
                if 'validation-summary' in response.text or 'field-validation-error' in response.text:
                    self.log("❌ Form validation errors detected", "ERROR")
                    soup = BeautifulSoup(response.text, 'html.parser')
                    errors = soup.find_all(class_='field-validation-error')
                    for error in errors:
                        self.log(f"  Validation error: {error.get_text().strip()}", "ERROR")
                    return False
                else:
                    self.log("⚠️ Form returned 200 but no clear error indication", "WARNING")
                    return False
            else:
                self.log(f"❌ Form submission failed with status: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"❌ Blog post creation test failed: {str(e)}", "ERROR")
            return False
            
    def test_blog_admin_list(self):
        """Test if created blog post appears in admin list"""
        try:
            admin_list_url = urljoin(self.base_url, '/Admin/Blog')
            self.log(f"Checking admin blog list: {admin_list_url}")
            
            response = self.session.get(admin_list_url)
            if response.status_code == 200:
                # Look for our test blog post
                if 'Test Blog Post' in response.text:
                    self.log("✅ Test blog post found in admin list")
                    return True
                else:
                    self.log("❌ Test blog post NOT found in admin list", "ERROR")
                    return False
            else:
                self.log(f"❌ Admin blog list not accessible: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"❌ Admin blog list test failed: {str(e)}", "ERROR")
            return False
            
    def test_public_blog_page(self):
        """Test if created blog post appears on public blog page"""
        try:
            public_blog_url = urljoin(self.base_url, '/Blog')
            self.log(f"Checking public blog page: {public_blog_url}")
            
            response = self.session.get(public_blog_url)
            if response.status_code == 200:
                # Look for our test blog post
                if 'Test Blog Post' in response.text:
                    self.log("✅ Test blog post found on public blog page")
                    return True
                else:
                    self.log("❌ Test blog post NOT found on public blog page", "ERROR")
                    return False
            else:
                self.log(f"❌ Public blog page not accessible: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"❌ Public blog page test failed: {str(e)}", "ERROR")
            return False
            
    def check_application_logs(self):
        """Check application logs for diagnostic messages"""
        try:
            log_file = '/var/log/aspnet-app.log'
            self.log(f"Checking application logs: {log_file}")
            
            if not os.path.exists(log_file):
                self.log("⚠️ Application log file not found at expected location", "WARNING")
                # Try alternative locations
                alternative_logs = [
                    '/var/log/supervisor/aas-web.log',
                    '/var/log/supervisor/aas-web.out.log',
                    '/var/log/supervisor/aas-web.err.log'
                ]
                
                for alt_log in alternative_logs:
                    if os.path.exists(alt_log):
                        log_file = alt_log
                        self.log(f"Found alternative log file: {log_file}")
                        break
                else:
                    self.log("❌ No application log files found", "ERROR")
                    return False
                    
            # Read recent log entries
            with open(log_file, 'r') as f:
                lines = f.readlines()
                
            # Look for diagnostic messages from the last few minutes
            recent_lines = lines[-200:]  # Last 200 lines
            diagnostic_found = False
            
            for line in recent_lines:
                if '===== BLOG POST CREATE ATTEMPT =====' in line:
                    diagnostic_found = True
                    self.log("✅ Found diagnostic log entry")
                    
                if any(keyword in line for keyword in ['TitleCs:', 'ContentCs:', 'ModelState.IsValid:', 'Published:']):
                    self.log(f"  Log: {line.strip()}")
                    
            if not diagnostic_found:
                self.log("⚠️ No diagnostic messages found in recent logs", "WARNING")
                
            return True
            
        except Exception as e:
            self.log(f"❌ Log checking failed: {str(e)}", "ERROR")
            return False
            
    def run_complete_test(self):
        """Run the complete blog post creation test flow"""
        self.log("=" * 60)
        self.log("STARTING ASP.NET CORE BLOG POST CREATION TEST")
        self.log("=" * 60)
        
        results = {}
        
        # Test 1: Server connectivity
        results['connectivity'] = self.test_server_connectivity()
        
        # Test 2: Admin login
        if results['connectivity']:
            results['login'] = self.test_admin_login()
        else:
            results['login'] = False
            
        # Test 3: Blog creation page access
        if results['login']:
            results['create_page'] = self.test_blog_create_page_access()[0]
        else:
            results['create_page'] = False
            
        # Test 4: Blog post creation
        if results['create_page']:
            results['post_creation'] = self.test_blog_post_creation()
        else:
            results['post_creation'] = False
            
        # Test 5: Admin list verification
        if results['post_creation']:
            results['admin_list'] = self.test_blog_admin_list()
        else:
            results['admin_list'] = False
            
        # Test 6: Public page verification
        if results['post_creation']:
            results['public_page'] = self.test_public_blog_page()
        else:
            results['public_page'] = False
            
        # Test 7: Log analysis
        results['logs'] = self.check_application_logs()
        
        # Summary
        self.log("=" * 60)
        self.log("TEST RESULTS SUMMARY")
        self.log("=" * 60)
        
        for test_name, result in results.items():
            status = "✅ PASS" if result else "❌ FAIL"
            self.log(f"{test_name.upper().replace('_', ' ')}: {status}")
            
        total_tests = len(results)
        passed_tests = sum(results.values())
        
        self.log(f"\nOVERALL: {passed_tests}/{total_tests} tests passed")
        
        if results['post_creation'] and not (results['admin_list'] or results['public_page']):
            self.log("\n⚠️ CRITICAL ISSUE: Blog post was created but doesn't appear anywhere!")
            self.log("This matches the reported bug - form submits but post isn't saved/visible")
            
        return results

def main():
    """Main test execution"""
    tester = BlogTestRunner()
    results = tester.run_complete_test()
    
    # Exit with appropriate code
    if results.get('post_creation', False):
        if results.get('admin_list', False) or results.get('public_page', False):
            exit(0)  # Success
        else:
            exit(1)  # Bug confirmed - post created but not visible
    else:
        exit(2)  # Creation failed

if __name__ == "__main__":
    main()