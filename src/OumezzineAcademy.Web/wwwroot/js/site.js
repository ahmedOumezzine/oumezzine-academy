(function () {
    'use strict';

    const toggle = document.querySelector('.nav-toggle');
    const nav = document.getElementById('mainNav');

    toggle?.addEventListener('click', function () {
        const open = nav?.classList.toggle('open') ?? false;
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'Fermer le menu' : 'Ouvrir le menu');
    });

    nav?.querySelectorAll('a').forEach(function (link) {
        link.addEventListener('click', function () {
            nav.classList.remove('open');
            toggle?.setAttribute('aria-expanded', 'false');
            toggle?.setAttribute('aria-label', 'Ouvrir le menu');
        });
    });

    document.querySelectorAll('.js-copy-url').forEach(function (button) {
        button.addEventListener('click', async function () {
            const original = button.innerHTML;
            try {
                await navigator.clipboard.writeText(button.closest('[data-share]')?.dataset.shareUrl || window.location.href);
                button.innerHTML = '<i class="bi bi-check2" aria-hidden="true"></i> Lien copié';
                setTimeout(function () { button.innerHTML = original; }, 1800);
            } catch {
                button.innerHTML = '<i class="bi bi-exclamation-circle" aria-hidden="true"></i> Copie impossible';
                setTimeout(function () { button.innerHTML = original; }, 1800);
            }
        });
    });

    document.querySelectorAll('.course-visual img').forEach(function (image) {
        image.addEventListener('error', function () {
            image.closest('.course-visual')?.classList.add('image-failed');
        }, { once: true });
    });

    document.querySelectorAll('.path-step-visual img').forEach(function (image) {
        image.addEventListener('error', function () {
            image.closest('.path-step-visual')?.classList.add('image-failed');
        }, { once: true });
    });
})();
