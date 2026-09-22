(() => {
    const form = document.querySelector('.path-course-picker');
    if (!form) return;

    const search = form.querySelector('[data-course-search]');
    const options = [...form.querySelectorAll('[data-course-option]')];
    const radios = [...form.querySelectorAll('[data-course-radio]')];
    const addButton = form.querySelector('[data-course-add]');
    const empty = form.querySelector('[data-course-empty]');

    function updateSelection() {
        const selected = radios.find(radio => radio.checked);
        addButton.disabled = !selected;
        options.forEach(option => option.classList.toggle('is-selected', option.contains(selected)));
    }

    function filterCourses() {
        const term = search.value.trim().toLocaleLowerCase();
        let visibleCount = 0;
        options.forEach(option => {
            const matches = option.dataset.searchText.toLocaleLowerCase().includes(term);
            option.hidden = !matches;
            if (matches) visibleCount++;
            else {
                const radio = option.querySelector('[data-course-radio]');
                if (radio.checked) radio.checked = false;
            }
        });
        empty.hidden = visibleCount !== 0;
        updateSelection();
    }

    search.addEventListener('input', filterCourses);
    radios.forEach(radio => radio.addEventListener('change', updateSelection));
    form.addEventListener('submit', event => {
        if (!radios.some(radio => radio.checked)) event.preventDefault();
    });
})();