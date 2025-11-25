function setLang(code) {
  const cookie =
    '.AspNetCore.Culture=C="' + code + '"|U="' + code + '"; path=/';
  document.cookie = cookie;
  location.reload();
}

let isSubmitting = false;

async function submitInquiry() {
  // Prevent multiple submissions
  if (isSubmitting) {
    console.log('Already submitting, please wait...');
    return;
  }
  
  const form = document.getElementById("inqForm");
  const submitBtn = document.querySelector('button[onclick="submitInquiry()"]');
  
  // Validate form
  if (!form.checkValidity()) {
    form.reportValidity();
    return;
  }
  
  // Set submitting state
  isSubmitting = true;
  
  // Disable button and show loading
  if (submitBtn) {
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sending...';
  }
  
  // Get form data - FormData works with ASP.NET MVC model binding
  const formData = new FormData(form);
  
  try {
    const res = await fetch("/Inquiries/Create", {
      method: "POST",
      body: formData
    });
    
    // Try to parse JSON, but handle non-JSON responses
    let result;
    const contentType = res.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
      result = await res.json();
    } else {
      // If not JSON, check status code
      const text = await res.text();
      console.log('Non-JSON response:', text);
      result = { success: res.ok, message: res.ok ? "Success" : "Error submitting inquiry" };
    }
    
    if (res.ok && result.success) {
      document.getElementById("inqOk").classList.remove("d-none");
      form.style.display = 'none'; // Hide form
      setTimeout(() => {
        closeInquiryForm();
        form.style.display = ''; // Show form again for next use
        form.reset();
        // Reset submitting state
        isSubmitting = false;
        if (submitBtn) {
          submitBtn.disabled = false;
          submitBtn.innerHTML = '<i class="bi bi-send me-2"></i>Send';
        }
      }, 2000);
    } else {
      // Show error message inline instead of alert
      showInlineError(result.message || result.errors?.join(', ') || "Failed to submit inquiry. Please try again.");
      // Reset submitting state
      isSubmitting = false;
      if (submitBtn) {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i class="bi bi-send me-2"></i>Send';
      }
    }
  } catch (error) {
    console.error('Error submitting inquiry:', error);
    showInlineError("An error occurred. Please try again later.");
    // Reset submitting state
    isSubmitting = false;
    if (submitBtn) {
      submitBtn.disabled = false;
      submitBtn.innerHTML = '<i class="bi bi-send me-2"></i>Send';
    }
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
