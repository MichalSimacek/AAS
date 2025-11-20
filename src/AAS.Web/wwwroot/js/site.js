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
      // Show error message inline instead of alert
      showInlineError(result.message || "Failed to submit inquiry. Please try again.");
    }
  } catch (error) {
    console.error('Error submitting inquiry:', error);
    showInlineError("An error occurred. Please try again later.");
  }
}

function showInlineError(message) {
  const errorDiv = document.getElementById("inqError");
  if (errorDiv) {
    errorDiv.textContent = message;
    errorDiv.classList.remove("d-none");
    setTimeout(() => {
      errorDiv.classList.add("d-none");
    }, 5000);
  }
}

// Disable all JavaScript animations on Identity pages to prevent flickering
document.addEventListener('DOMContentLoaded', function() {
  // Check if we're on an Identity page
  const isIdentityPage = window.location.pathname.includes('/Identity/');

  if (isIdentityPage) {
    // Do nothing - let CSS handle everything for stable layout
    return;
  }

  // Add smooth transitions only to non-Identity interactive elements
  const interactiveElements = document.querySelectorAll('button:not(.navbar-toggler), a:not(.nav-link), .btn');
  interactiveElements.forEach(el => {
    if (!el.style.transition && !el.closest('.identity-card') && !el.closest('.manage-account-container')) {
      el.style.transition = 'color 0.2s ease, background-color 0.2s ease, transform 0.2s ease';
    }
  });
});
