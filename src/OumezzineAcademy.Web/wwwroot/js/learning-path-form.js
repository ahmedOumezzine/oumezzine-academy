(() => {
    const form = document.querySelector('.learning-path-form');
    if (!form) return;

    const get = name => form.querySelector(`[name="${name}"]`);
    const slug = value => String(value || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    const update = () => {
        const english = form.querySelector('.learning-path-tabs .is-active')?.dataset.pathTab === 'path-en';
        const prefix = english ? 'English' : 'French';
        form.querySelector('[data-path-preview-title]').textContent = get(`${prefix}.Title`)?.value || 'Titre du parcours';
        const summary = get(`${prefix}.Summary`);
        form.querySelector('[data-path-preview-summary]').textContent = summary?.value?.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim() || 'Le résumé apparaîtra ici.';
        form.querySelector('[data-path-preview-category]').textContent = get('CategoryId')?.selectedOptions[0]?.textContent || 'Catégorie';
        form.querySelector('[data-path-preview-level]').textContent = get('Level')?.selectedOptions[0]?.textContent || '';
    };

    form.querySelectorAll('[data-path-slug-target]').forEach(button => button.addEventListener('click', () => {
        const target = get(button.dataset.pathSlugTarget);
        if (target && !target.value) target.value = slug(get(button.dataset.pathSlugFrom)?.value);
    }));

    form.querySelectorAll('.learning-path-tabs button').forEach(tab => tab.addEventListener('click', () => {
        form.querySelectorAll('.learning-path-tabs button').forEach(button => {
            button.classList.toggle('is-active', button === tab);
            button.setAttribute('aria-selected', button === tab ? 'true' : 'false');
        });
        form.querySelectorAll('.learning-path-tab-panel').forEach(panel => { panel.hidden = panel.id !== tab.dataset.pathTab; });
        form.dispatchEvent(new CustomEvent('path:tabchange'));
        update();
    }));

    const input = form.querySelector('[data-path-image-input]');
    const dropzone = form.querySelector('[data-path-dropzone]');
    const previews = [...form.querySelectorAll('[data-path-image-preview], [data-path-preview-image]')];
    let objectUrl = null;
    const setImage = url => previews.forEach(preview => {
        preview.onerror = () => {
            preview.hidden = true;
            const placeholder = preview.parentElement.querySelector('[data-path-image-placeholder], [data-path-preview-placeholder]');
            if (placeholder) placeholder.hidden = false;
        };
        preview.onload = () => {
            preview.hidden = false;
            const placeholder = preview.parentElement.querySelector('[data-path-image-placeholder], [data-path-preview-placeholder]');
            if (placeholder) placeholder.hidden = true;
        };
        preview.src = url || '';
        if (!url) {
            preview.hidden = true;
            const placeholder = preview.parentElement.querySelector('[data-path-image-placeholder], [data-path-preview-placeholder]');
            if (placeholder) placeholder.hidden = false;
        }
    });
    const acceptFile = file => {
        if (!file) return;
        if (!/\.(jpe?g|png|webp)$/i.test(file.name) || !/^image\/(jpeg|png|webp)$/i.test(file.type) || file.size > 5 * 1024 * 1024) {
            alert('Format non supporté ou image trop volumineuse.');
            input.value = '';
            return;
        }
        if (objectUrl) URL.revokeObjectURL(objectUrl);
        objectUrl = URL.createObjectURL(file);
        setImage(objectUrl);
        form.querySelector('[data-path-file-name]').textContent = file.name;
    };
    input?.addEventListener('change', () => acceptFile(input.files?.[0]));
    if (dropzone && input) {
        for (const eventName of ['dragenter', 'dragover']) dropzone.addEventListener(eventName, event => {
            event.preventDefault();
            dropzone.classList.add('is-dragover');
        });
        for (const eventName of ['dragleave', 'drop']) dropzone.addEventListener(eventName, event => {
            event.preventDefault();
            dropzone.classList.remove('is-dragover');
        });
        dropzone.addEventListener('drop', event => {
            const file = event.dataTransfer.files?.[0];
            if (!file) return;
            const transfer = new DataTransfer();
            transfer.items.add(file);
            input.files = transfer.files;
            acceptFile(file);
        });
    }
    previews.forEach(preview => preview.addEventListener('error', () => {
        preview.hidden = true;
        const placeholder = preview.parentElement.querySelector('[data-path-image-placeholder], [data-path-preview-placeholder]');
        if (placeholder) placeholder.hidden = false;
    }));
    previews.forEach(preview => {
        if (preview.complete && preview.naturalWidth === 0 && preview.getAttribute('src')) {
            preview.hidden = true;
            const placeholder = preview.parentElement.querySelector('[data-path-image-placeholder], [data-path-preview-placeholder]');
            if (placeholder) placeholder.hidden = false;
        }
    });
    form.querySelectorAll('input,textarea,select').forEach(field => field.addEventListener('input', update));
    form.addEventListener('submit', () => {
        if (objectUrl) URL.revokeObjectURL(objectUrl);
    });
    update();
})();