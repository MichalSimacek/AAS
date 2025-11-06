function setLang(code) {
  const cookie =
    '.AspNetCore.Culture=C="' + code + '"|U="' + code + '"; path=/';
  document.cookie = cookie;
  location.reload();
}

async function submitInquiry() {
  const form = document.getElementById("inqForm");
  const data = new FormData(form);
  const res = await fetch("/Inquiries/Create", {
    method: "POST",
    body: data,
    headers: { "X-Requested-With": "fetch" },
  });
  if (res.ok) {
    document.getElementById("inqOk").classList.remove("d-none");
    setTimeout(() => location.reload(), 1200);
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
