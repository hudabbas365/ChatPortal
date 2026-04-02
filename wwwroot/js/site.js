// ChatPortal - Site JavaScript

$(document).ready(function () {
    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        $('.alert.alert-success').fadeOut('slow');
    }, 5000);

    // Active nav highlighting
    const currentPath = window.location.pathname.toLowerCase();
    $('.navbar-nav .nav-link').each(function () {
        const href = $(this).attr('href')?.toLowerCase();
        if (href && href !== '/' && currentPath.startsWith(href)) {
            $(this).addClass('active');
        }
    });

    // Tooltip initialization
    const tooltipElems = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipElems.forEach(el => new bootstrap.Tooltip(el));
});
