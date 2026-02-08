// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Global site JS: modern interactions

document.addEventListener('DOMContentLoaded', function () {
    // Navbar shrink on scroll
    const navbar = document.querySelector('.navbar');
    function onScroll() {
        if (!navbar) return;
        if (window.scrollY > 50) navbar.classList.add('navbar-shrink');
        else navbar.classList.remove('navbar-shrink');
    }
    window.addEventListener('scroll', onScroll);
    onScroll();

    // Reveal elements on scroll
    const reveals = document.querySelectorAll('.reveal-on-scroll');
    const revealObserver = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) {
                e.target.classList.add('revealed');
                revealObserver.unobserve(e.target);
            }
        });
    }, { threshold: 0.15 });
    reveals.forEach(r => revealObserver.observe(r));

    // Simple toast API
    window.showToast = function (message, type = 'info', timeout = 4000) {
        const container = document.getElementById('toast-container');
        if (!container) return;
        const toast = document.createElement('div');
        toast.className = `app-toast app-toast-${type}`;
        toast.innerHTML = `<div class="toast-body">${message}</div>`;
        container.appendChild(toast);
        // animate in
        requestAnimationFrame(() => toast.classList.add('visible'));
        setTimeout(() => {
            toast.classList.remove('visible');
            setTimeout(() => container.removeChild(toast), 300);
        }, timeout);
    };

    // Progressive enhancement: highlight invalid fields on submit
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', (e) => {
            // mark touched inputs
            form.querySelectorAll('input,textarea,select').forEach(i => i.classList.add('touched'));
        });
    });
});

// Expose a helper for password strength bar update (used by pages)
window.updatePasswordStrength = function (elementId, valuePct, label) {
    const bar = document.getElementById(elementId);
    if (!bar) return;
    bar.style.width = valuePct + '%';
    bar.setAttribute('aria-valuenow', valuePct);
    bar.classList.remove('bg-danger', 'bg-warning', 'bg-success');
    if (valuePct < 40) bar.classList.add('bg-danger');
    else if (valuePct < 70) bar.classList.add('bg-warning');
    else bar.classList.add('bg-success');
    if (label) {
        const lbl = bar.closest('.pw-wrap')?.querySelector('.pw-label');
        if (lbl) lbl.textContent = label;
    }
};
