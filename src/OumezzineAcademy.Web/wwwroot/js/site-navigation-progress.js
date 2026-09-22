(function () {
    'use strict';

    const root = document.documentElement;

    function startProgress() {
        root.classList.remove('is-navigation-complete');
        root.classList.add('is-navigating');
    }

    function isSameOriginNavigation(url) {
        try {
            return new URL(url, window.location.href).origin === window.location.origin;
        } catch {
            return false;
        }
    }

    document.addEventListener('click', function (event) {
        if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

        const link = event.target instanceof Element ? event.target.closest('a[href]') : null;
        if (!link || link.hasAttribute('download') || link.target && link.target.toLowerCase() !== '_self') return;

        const href = link.getAttribute('href')?.trim();
        if (!href || href.startsWith('#') || /^(mailto:|tel:|javascript:)/i.test(href)) return;
        if (!isSameOriginNavigation(link.href)) return;

        startProgress();
    });

    document.addEventListener('submit', function (event) {
        if (event.defaultPrevented) return;

        const form = event.target;
        if (!(form instanceof HTMLFormElement)) return;

        const submitter = event.submitter;
        const target = submitter?.formTarget || form.target;
        const action = submitter?.formAction || form.action || window.location.href;
        if (target && target.toLowerCase() !== '_self') return;
        if (!isSameOriginNavigation(action)) return;

        startProgress();
    });

    window.addEventListener('pagehide', function () {
        if (root.classList.contains('is-navigating')) root.classList.add('is-navigation-complete');
    });

    window.addEventListener('pageshow', function () {
        root.classList.remove('is-navigation-complete', 'is-navigating');
    });
})();