(() => {
    const form = document.querySelector('.learning-path-form');
    if (!form || !window.CKEDITOR) return;

    const editors = new Map();
    const fields = [...form.querySelectorAll('[data-path-summary-editor]')];
    const sync = (field, editor) => {
        field.value = editor.getData();
        field.dispatchEvent(new Event('input', { bubbles: true }));
    };
    const initialize = async field => {
        if (editors.has(field) || field.closest('[hidden]')) return;
        editors.set(field, null);
        const wrapper = field.closest('[data-editor-wrapper]');
        try {
            const CK = window.CKEDITOR;
            const editor = await CK.ClassicEditor.create(field, {
                licenseKey: 'GPL',
                plugins: [CK.Essentials, CK.Paragraph, CK.Heading, CK.Bold, CK.Italic,
                    CK.Underline, CK.List, CK.BlockQuote, CK.Link, CK.AutoLink,
                    CK.RemoveFormat, CK.GeneralHtmlSupport],
                language: { ui: 'fr', content: field.dataset.contentLanguage || 'fr' },
                translations: [window.CKEDITOR_TRANSLATIONS].filter(Boolean),
                toolbar: {
                    items: ['undo', 'redo', '|', 'heading', '|', 'bold', 'italic', 'underline',
                        '|', 'bulletedList', 'numberedList', 'link', 'blockQuote', '|', 'removeFormat'],
                    shouldNotGroupWhenFull: false
                },
                heading: { options: [
                    { model: 'paragraph', title: 'Paragraphe', class: 'ck-heading_paragraph' },
                    { model: 'heading2', view: 'h2', title: 'Titre H2', class: 'ck-heading_heading2' },
                    { model: 'heading3', view: 'h3', title: 'Titre H3', class: 'ck-heading_heading3' },
                    { model: 'heading4', view: 'h4', title: 'Titre H4', class: 'ck-heading_heading4' }
                ] },
                link: { addTargetToExternalLinks: true, defaultProtocol: 'https://' },
                htmlSupport: { allow: [{ name: /^(p|h2|h3|h4|strong|em|u|ul|ol|li|blockquote|a|br)$/, attributes: ['href', 'title', 'target', 'rel'] }] }
            });
            editors.set(field, editor);
            editor.model.document.on('change:data', () => sync(field, editor));
            editor.editing.view.change(writer => writer.setAttribute('aria-label', field.getAttribute('aria-label'), editor.editing.view.document.getRoot()));
            wrapper.querySelector('[data-editor-error]').hidden = true;
            sync(field, editor);
        } catch (error) {
            editors.delete(field);
            wrapper.querySelector('[data-editor-error]').hidden = false;
            console.error('Learning path summary editor initialization failed.', error);
        }
    };
    const initializeVisible = () => fields.forEach(initialize);
    form.addEventListener('path:tabchange', initializeVisible);
    form.addEventListener('submit', () => editors.forEach((editor, field) => { if (editor) sync(field, editor); }));
    initializeVisible();
})();