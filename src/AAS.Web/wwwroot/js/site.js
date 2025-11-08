function setLang(code) {
  const cookie =
    '.AspNetCore.Culture=C="' + code + '"|U="' + code + '"; path=/';
  document.cookie = cookie;
  location.reload();
}

async function submitInquiry() {
  const form = document.getElementById("inqForm");
  const data = new FormData(form);
  
  // SECURITY FIX: Include anti-forgery token in request
  const token = form.querySelector('input[name="__RequestVerificationToken"]');
  if (token) {
    data.append('__RequestVerificationToken', token.value);
  }
  
  try {
    const res = await fetch("/Inquiries/Create", {
      method: "POST",
      body: data,
      headers: { 
        "X-Requested-With": "fetch"
      },
    });
    
    const result = await res.json();
    
    if (res.ok && result.success) {
      document.getElementById("inqOk").classList.remove("d-none");
      setTimeout(() => {
        const modal = bootstrap.Modal.getInstance(document.getElementById('interestModal'));
        if (modal) modal.hide();
        form.reset();
        document.getElementById("inqOk").classList.add("d-none");
      }, 1500);
    } else {
      alert(result.message || "Failed to submit inquiry. Please try again.");
    }
  } catch (error) {
    console.error('Error submitting inquiry:', error);
    alert("An error occurred. Please try again later.");
  }
}

// Aristocratic styling for Identity pages
document.addEventListener('DOMContentLoaded', function() {
  // Check if we're on an Identity page
  const isIdentityPage = window.location.pathname.includes('/Identity/');

  if (isIdentityPage) {
    // Find the main content area
    const main = document.querySelector('main');
    if (main) {
      // Wrap Identity forms in auth-container
      const forms = main.querySelectorAll('form');
      forms.forEach(form => {
        if (!form.closest('.auth-container')) {
          const wrapper = document.createElement('div');
          wrapper.className = 'auth-container fade-in';
          form.parentNode.insertBefore(wrapper, form);
          wrapper.appendChild(form);

          // Move any headings or validation summaries into the wrapper
          const siblings = [];
          let prev = wrapper.previousElementSibling;
          while (prev) {
            if (prev.tagName === 'H1' || prev.tagName === 'H2' ||
                prev.classList.contains('validation-summary-errors') ||
                prev.classList.contains('text-danger')) {
              siblings.unshift(prev);
            }
            prev = prev.previousElementSibling;
          }
          siblings.forEach(el => wrapper.insertBefore(el, wrapper.firstChild));
        }
      });

      // Style any remaining content
      const directChildren = Array.from(main.children);
      directChildren.forEach(child => {
        if (!child.classList.contains('auth-container') &&
            !child.classList.contains('container')) {
          child.style.maxWidth = '480px';
          child.style.margin = '2rem auto';
        }
      });
    }
  }

  // Add smooth transitions to all interactive elements
  const interactiveElements = document.querySelectorAll('button, a, .card, .btn');
  interactiveElements.forEach(el => {
    if (!el.style.transition) {
      el.style.transition = 'all 0.3s ease';
    }
  });
});
