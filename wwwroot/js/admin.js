$(document).ready(function () {
    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        $('.alert').fadeOut('slow');
    }, 5000);

    // Confirm delete actions
    $('.btn-delete').on('click', function (e) {
        if (!confirm('Are you sure you want to delete this item?')) {
            e.preventDefault();
        }
    });

    // Highlight active menu item
    var currentPath = window.location.pathname;
    $('.nav-sidebar a').each(function () {
        var href = $(this).attr('href');
        if (currentPath.indexOf(href) !== -1 && href !== '/') {
            $(this).addClass('active');
        }
    });
});