(() => {
    const input = document.querySelector('[data-thumbnail-input]');
    const preview = document.querySelector('#course-thumbnail-preview');
    const previewBox = document.querySelector('.course-thumbnail-preview');
    const fileName = document.querySelector('[data-thumbnail-name]');

    if (input && preview && previewBox) {
        preview.addEventListener('error', () => {
            preview.classList.add('is-hidden');
            previewBox.dataset.thumbnailEmpty = 'true';
        });
        input.addEventListener('change', () => {
            const file = input.files && input.files[0];
            if (!file) return;
            preview.src = URL.createObjectURL(file);
            preview.alt = `Aperçu de ${file.name}`;
            preview.classList.remove('is-hidden');
            previewBox.dataset.thumbnailEmpty = 'false';
            if (fileName) fileName.textContent = file.name;
        });
    }

    document.querySelectorAll('[data-course-tab]').forEach(tab => {
        tab.addEventListener('click', () => {
            const target = document.getElementById(tab.dataset.courseTab);
            document.querySelectorAll('[data-course-tab]').forEach(item => {
                const active = item === tab;
                item.classList.toggle('is-active', active);
                item.setAttribute('aria-selected', active ? 'true' : 'false');
            });
            document.querySelectorAll('.course-tab-panel').forEach(panel => {
                panel.hidden = panel !== target;
                panel.classList.toggle('is-active', panel === target);
            });
        });
    });

    document.querySelectorAll('[data-slug-target]').forEach(button => {
        button.addEventListener('click', () => {
            const source = document.querySelector(`[name="${button.dataset.slugFrom}"]`);
            const target = document.querySelector(`[name="${button.dataset.slugTarget}"]`);
            if (!source || !target || target.value.trim()) return;
            target.value = source.value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
        });
    });

    const search = document.querySelector('[data-prerequisite-search]');
    if (search) {
        search.addEventListener('input', () => {
            const query = search.value.trim().toLowerCase();
            document.querySelectorAll('[data-prerequisite-item]').forEach(item => {
                item.hidden = query && !item.textContent.toLowerCase().includes(query);
            });
        });
    }

    const deleteButton = document.querySelector('[data-delete-thumbnail]');
    if (deleteButton) {
        deleteButton.addEventListener('click', event => {
            if (!window.confirm('Supprimer la couverture actuelle ?')) event.preventDefault();
        });
    }
})();
