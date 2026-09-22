(() => {
    const form = document.querySelector('[data-rich-content-form]');
    if (!form) return;
    const tabs = [...form.querySelectorAll('[data-lesson-tab]')];
    tabs.forEach((tab, index) => {
        tab.id = `${tab.dataset.lessonTab}-tab`;
        tab.setAttribute('aria-controls', tab.dataset.lessonTab);
        document.getElementById(tab.dataset.lessonTab)?.setAttribute('aria-labelledby', tab.id);
        tab.addEventListener('click', () => { tabs.forEach(item => { const active = item === tab; item.classList.toggle('active', active); item.setAttribute('aria-selected', active); item.tabIndex = active ? 0 : -1; }); form.querySelectorAll('.lesson-tab-panel').forEach(panel => panel.hidden = panel.id !== tab.dataset.lessonTab); form.dispatchEvent(new Event('content:tabchange')); });
        tab.addEventListener('keydown', event => { const next = event.key === 'ArrowRight' ? (index + 1) % tabs.length : event.key === 'ArrowLeft' ? (index + tabs.length - 1) % tabs.length : -1; if (next >= 0) { event.preventDefault(); tabs[next].click(); tabs[next].focus(); } });
    });
})();
