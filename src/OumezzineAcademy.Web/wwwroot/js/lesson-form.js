(() => {
    const form = document.querySelector('.lesson-concept-form');
    if (!form) return;

    const get = name => form.querySelector(`[name="${name}"]`);
    const prefixFor = language => language === 'fr' ? 'French' : 'English';
    const names = language => { const prefix = prefixFor(language); return { prefix, title: `${prefix}.Title`, summary: `${prefix}.Summary`, content: `${prefix}.ContentHtml`, video: `${prefix}.VideoUrl`, document: `${prefix}.DocumentUrl` }; };
    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[character]));
    // Preview only; the unchanged server sanitizer remains authoritative on save.
    const safeHtml = html => {
        const template = document.createElement('template');
        template.innerHTML = html || '';
        template.content.querySelectorAll('script,style,iframe,object,embed,svg,math').forEach(element => element.remove());
        const tags = new Set('P H2 H3 H4 UL OL LI STRONG EM U A BLOCKQUOTE PRE CODE IMG FIGURE FIGCAPTION BR TABLE THEAD TBODY TFOOT TR TH TD'.split(' '));
        const attributes = new Set('href title target rel src alt width height loading class'.split(' '));
        template.content.querySelectorAll('*').forEach(element => {
            if (!tags.has(element.tagName)) { element.replaceWith(...element.childNodes); return; }
            [...element.attributes].forEach(attribute => {
                if (!attributes.has(attribute.name)) { element.removeAttribute(attribute.name); return; }
                if (['href', 'src'].includes(attribute.name)) {
                    try {
                        const protocol = new URL(attribute.value, document.baseURI).protocol;
                        if (!['http:', 'https:', ...(attribute.name === 'href' ? ['mailto:'] : [])].includes(protocol)) element.removeAttribute(attribute.name);
                    } catch { element.removeAttribute(attribute.name); }
                }
            });
            if (element.tagName === 'A' && element.target === '_blank') element.rel = 'noopener noreferrer';
        });
        return template.innerHTML || '<p>Le contenu apparaîtra ici.</p>';
    };
    const previewLanguage = () => form.querySelector('[data-preview-language].is-active')?.dataset.previewLanguage || 'fr';
    const updatePreview = () => { const current = names(previewLanguage()); const html = safeHtml(get(current.content)?.value); form.querySelector('[data-preview-title]').textContent = get(current.title)?.value.trim() || 'Titre de la leçon'; form.querySelector('[data-preview-summary]').textContent = get(current.summary)?.value.trim() || 'Le résumé apparaîtra ici.'; form.querySelector('[data-preview-html]').innerHTML = html; const image = form.querySelector('[data-preview-image]'); const source = new DOMParser().parseFromString(html, 'text/html').querySelector('img')?.getAttribute('src'); if (image) { image.hidden = !source; if (source) image.src = source; } form.querySelector('[data-preview-duration]').textContent = get('DurationMinutes')?.value ? `${get('DurationMinutes').value} min` : '—'; form.querySelector('[data-preview-video-pill]').classList.toggle('preview-active', !!get(current.video)?.value.trim()); form.querySelector('[data-preview-document-pill]').classList.toggle('preview-active', !!get(current.document)?.value.trim()); };
    const updateSeoCounters = () => form.querySelectorAll('[data-count-for]').forEach(counter => { const field = get(counter.dataset.countFor); if (field) counter.textContent = `${field.value.length} / ${field.maxLength}`; });
    const renderLocalFile = file => { const list = form.querySelector('[data-lesson-media-list]'); if (!list || !file) return; const url = URL.createObjectURL(file); list.innerHTML = `<div class="lesson-media-item"><img src="${url}" alt="Aperçu de ${escapeHtml(file.name)}"><div><strong>${escapeHtml(file.name)}</strong><small>${Math.max(1, Math.round(file.size / 1024))} KB, prêt à l’enregistrement</small><span class="lesson-media-note">L’image sera envoyée avec la leçon.</span></div><button type="button" class="lesson-media-delete" data-remove-local-image aria-label="Retirer l’image">×</button></div>`; const preview = form.querySelector('[data-preview-image]'); if (preview) { preview.src = url; preview.hidden = false; } list.querySelector('[data-remove-local-image]').addEventListener('click', () => { URL.revokeObjectURL(url); const input = form.querySelector('[data-lesson-image-input]'); if (input) input.value = ''; list.innerHTML = '<p class="lesson-media-empty">Aucune image sélectionnée.</p>'; if (preview) { preview.hidden = true; preview.removeAttribute('src'); } }); };
    const imageInput = form.querySelector('[data-lesson-image-input]');
    const uploadZone = form.querySelector('.lesson-upload-zone');
    const acceptFile = file => file && /\.(jpe?g|png|webp)$/i.test(file.name) && file.size <= 5 * 1024 * 1024;
    const chooseFile = file => { if (!acceptFile(file)) { if (file) window.alert('Sélectionnez une image JPG, PNG ou WEBP de 5 Mo maximum.'); return; } const transfer = new DataTransfer(); transfer.items.add(file); imageInput.files = transfer.files; renderLocalFile(file); };
    imageInput?.addEventListener('change', () => chooseFile(imageInput.files?.[0]));
    ['dragenter', 'dragover'].forEach(eventName => uploadZone?.addEventListener(eventName, event => { event.preventDefault(); uploadZone.classList.add('is-dragover'); }));
    ['dragleave', 'drop'].forEach(eventName => uploadZone?.addEventListener(eventName, event => { event.preventDefault(); uploadZone.classList.remove('is-dragover'); if (eventName === 'drop') chooseFile(event.dataTransfer.files?.[0]); }));
    form.querySelectorAll('[data-lesson-tab]').forEach(tab => tab.addEventListener('click', () => { form.querySelectorAll('[data-lesson-tab]').forEach(item => { const active = item === tab; item.classList.toggle('active', active); item.setAttribute('aria-selected', active); }); form.querySelectorAll('.lesson-tab-panel').forEach(panel => panel.hidden = panel.id !== tab.dataset.lessonTab); form.dispatchEvent(new Event('lesson:tabchange')); updatePreview(); }));
    form.querySelectorAll('[data-seo-tab]').forEach(tab => tab.addEventListener('click', () => { form.querySelectorAll('[data-seo-tab]').forEach(item => { const active = item === tab; item.classList.toggle('is-active', active); item.setAttribute('aria-selected', active); }); form.querySelectorAll('.lesson-seo-panel').forEach(panel => panel.hidden = panel.id !== tab.dataset.seoTab); }));
    form.querySelectorAll('[data-preview-language]').forEach(tab => tab.addEventListener('click', () => { form.querySelectorAll('[data-preview-language]').forEach(item => item.classList.toggle('is-active', item === tab)); updatePreview(); }));
    form.querySelectorAll('[data-count-for]').forEach(counter => get(counter.dataset.countFor)?.addEventListener('input', updateSeoCounters));
    const lessonTabs = [...form.querySelectorAll('[data-lesson-tab]')];
    lessonTabs.forEach((tab, index) => {
        tab.id = `${tab.dataset.lessonTab}-tab`;
        tab.tabIndex = tab.getAttribute('aria-selected') === 'true' ? 0 : -1;
        document.getElementById(tab.dataset.lessonTab)?.setAttribute('aria-labelledby', tab.id);
        tab.addEventListener('click', () => lessonTabs.forEach(item => item.tabIndex = item === tab ? 0 : -1));
        tab.addEventListener('keydown', event => {
            const next = { ArrowRight: (index + 1) % lessonTabs.length, ArrowLeft: (index + lessonTabs.length - 1) % lessonTabs.length, Home: 0, End: lessonTabs.length - 1 }[event.key];
            if (next === undefined) return;
            event.preventDefault();
            lessonTabs[next].click();
            lessonTabs[next].focus();
        });
    });
    form.querySelectorAll('[data-slug-target]').forEach(button => button.addEventListener('click', () => { const source = get(button.dataset.slugFrom); const target = get(button.dataset.slugTarget); if (!source || !target || target.value.trim()) return; target.value = source.value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''); }));
    form.querySelectorAll('input,textarea,select').forEach(input => input.addEventListener('input', updatePreview));
    updateSeoCounters();
    updatePreview();
})();