// CPTMS - Campus Project & Thesis Management System
// Client-side initialization scripts

document.addEventListener('DOMContentLoaded', function () {
    // Auto-dismiss alert messages after 5 seconds if present
    const alertElements = document.querySelectorAll('.alert-dismissible');
    alertElements.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});
