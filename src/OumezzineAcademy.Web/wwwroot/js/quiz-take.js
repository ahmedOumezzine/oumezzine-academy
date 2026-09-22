document.addEventListener('DOMContentLoaded', () => {
    const form = document.querySelector('.quiz-form');
    if (!form) return;
    const radios = form.querySelectorAll('input[type="radio"]');
    const answered = form.querySelector('[data-quiz-answered]');
    const progress = form.querySelector('[data-quiz-progress]');
    const update = () => {
        const groups = new Set([...radios].filter(radio => radio.checked).map(radio => radio.name));
        const total = new Set([...radios].map(radio => radio.name)).size;
        const value = total ? Math.round(groups.size / total * 100) : 0;
        if (answered) answered.textContent = groups.size;
        if (progress) progress.style.width = `${value}%`;
    };
    radios.forEach(radio => radio.addEventListener('change', update));
    update();
});