(() => {
    const form = document.querySelector('.course-form');
    if (!form) return;
    form.querySelectorAll('[data-quiz-tab]').forEach(tab => tab.addEventListener('click', () => { form.querySelectorAll('[data-quiz-tab]').forEach(item => { const active = item === tab; item.classList.toggle('is-active', active); item.setAttribute('aria-selected', active); }); form.querySelectorAll('.course-tab-panel').forEach(panel => panel.hidden = panel.id !== tab.dataset.quizTab); }));
    form.querySelectorAll('[data-slug-target]').forEach(button => button.addEventListener('click', () => { const source = form.querySelector(`[name="${button.dataset.slugFrom}"]`); const target = form.querySelector(`[name="${button.dataset.slugTarget}"]`); if (!source || !target || target.value.trim()) return; target.value = source.value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''); }));
})();