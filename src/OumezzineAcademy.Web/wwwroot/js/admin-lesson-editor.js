(() => {
    const form = document.querySelector('[data-rich-content-form], .lesson-concept-form');
    if (!form) return;

    const editors = new Map();
    const fields = [...form.querySelectorAll('[data-lesson-editor], [data-rich-text-editor]')];
    const entityLabel = form.dataset.contentEntity || 'la leçon';
    const photoArea = form.dataset.photoArea || 'lessons';
    const uploadUrl = form.dataset.photoUploadUrl;
    const photoStatus = form.querySelector('[data-photo-status]');
    let pendingUploads = 0;
    let submitStates = [];
    const showPhotoStatus = (message, failed = false) => {
        photoStatus.textContent = message;
        photoStatus.hidden = !message;
        photoStatus.classList.toggle('field-error', failed);
    };
    const setUploading = delta => {
        if (pendingUploads === 0 && delta > 0) {
            submitStates = [...form.querySelectorAll('button[type="submit"]')].map(button => [button, button.disabled]);
            submitStates.forEach(([button]) => button.disabled = true);
        }
        pendingUploads += delta;
        if (pendingUploads === 0) submitStates.forEach(([button, disabled]) => button.disabled = disabled);
    };
    const uploadPhotos = async (editor, files) => {
        if (!uploadUrl || !files.length || editor.isReadOnly) return;
        setUploading(1);
        editor.enableReadOnlyMode('lesson-photo-upload');
        try {
            for (const file of files) {
                if (!/\.(jpe?g|png|webp)$/i.test(file.name) || !file.size || file.size > 5 * 1024 * 1024)
                    throw new Error('Choisissez une photo JPG, PNG ou WEBP de 5 Mo maximum.');
                showPhotoStatus(`Envoi de ${file.name}…`);
                const body = new FormData();
                body.append('file', file);
                body.append('__RequestVerificationToken', form.querySelector('[name="__RequestVerificationToken"]').value);
                const response = await fetch(uploadUrl, { method: 'POST', body, credentials: 'same-origin' });
                if (!response.ok || response.redirected)
                    throw new Error('La photo n’a pas pu être envoyée. Vérifiez son format ou reconnectez-vous, puis réessayez.');
                const result = await response.json();
                const url = new URL(result.url, location.origin);
                const prefix = `/uploads/${photoArea}/${form.querySelector('[name="Id"]').value.toLowerCase()}/`;
                if (url.origin !== location.origin || !url.pathname.startsWith(prefix))
                    throw new Error('Le serveur a renvoyé une adresse de photo invalide.');
                // Insert only the validated server URL: no blob/base64 preview or transient HTML is needed.
                editor.disableReadOnlyMode('lesson-photo-upload');
                editor.execute('insertImage', { source: [{ src: url.pathname, alt: result.alt || file.name }] });
                editor.enableReadOnlyMode('lesson-photo-upload');
            }
            showPhotoStatus(`Photo(s) ajoutée(s). Enregistrez les modifications de ${entityLabel}.`);
        } catch (error) {
            showPhotoStatus(error.message || 'Impossible d’envoyer la photo. Réessayez.', true);
        } finally {
            editor.disableReadOnlyMode('lesson-photo-upload');
            setUploading(-1);
            editor.editing.view.focus();
        }
    };
    const sync = (field, editor) => {
        field.value = editor.getData();
        field.dispatchEvent(new Event('input', { bubbles: true }));
    };
    const initialize = async field => {
        // Reserve the field before awaiting creation: rapid tab switches must not create two editors.
        if (editors.has(field) || field.closest('[hidden]')) return;
        editors.set(field, null);
        const wrapper = field.closest('[data-editor-wrapper]');
        try {
            const CK = window.CKEDITOR;
            class LessonPhotoUpload extends CK.Plugin {
                init() {
                    const editor = this.editor;
                    // Keep width/height as HTML attributes. CKEditor's default size converter also
                    // emits an inline aspect-ratio style, which this application's CSP forbids.
                    for (const pipeline of ['editingDowncast', 'dataDowncast']) {
                        editor.conversion.for(pipeline).add(dispatcher => {
                            for (const imageType of ['imageBlock', 'imageInline']) {
                                for (const attribute of ['width', 'height']) {
                                    dispatcher.on(`attribute:${attribute}:${imageType}`, (event, data, conversion) => {
                                        if (!conversion.consumable.consume(data.item, event.name)) return;
                                        const image = editor.plugins.get('ImageUtils').findViewImgElement(conversion.mapper.toViewElement(data.item));
                                        if (data.attributeNewValue === null) conversion.writer.removeAttribute(attribute, image);
                                        else conversion.writer.setAttribute(attribute, data.attributeNewValue, image);
                                    }, { priority: 'high' });
                                }
                            }
                        });
                    }
                    editor.ui.componentFactory.add('lessonPhotoUpload', locale => {
                        const button = new CK.FileDialogButtonView(locale);
                        button.set({ label: 'Insérer une photo', withText: true,
                            acceptedType: '.jpg,.jpeg,.png,.webp', allowMultipleFiles: true,
                            tooltip: uploadUrl ? 'JPG, PNG ou WEBP — 5 Mo par photo' : `Enregistrez ${entityLabel} pour ajouter des photos` });
                        button.bind('isEnabled').to(editor, 'isReadOnly', readOnly => !!uploadUrl && !readOnly);
                        button.on('done', (_, files) => uploadPhotos(editor, [...files]));
                        return button;
                    });
                }
            }
            const editor = await CK.ClassicEditor.create(field, {
                licenseKey: 'GPL',
                plugins: [CK.Essentials, CK.Paragraph, CK.Heading, CK.Bold, CK.Italic,
                    CK.Underline, CK.List, CK.BlockQuote, CK.Link, CK.AutoLink,
                    CK.RemoveFormat, CK.Code, CK.CodeBlock, CK.Table, CK.TableToolbar,
                    CK.GeneralHtmlSupport, CK.WordCount, CK.ImageBlock, CK.ImageInline,
                    CK.ImageCaption, CK.ImageTextAlternative, CK.ImageToolbar, LessonPhotoUpload],
                language: { ui: 'fr', content: field.dataset.contentLanguage },
                translations: [window.CKEDITOR_TRANSLATIONS],
                wordCount: { onUpdate: stats => {
                    wrapper.querySelector('[data-word-count]').textContent = `${stats.words} mot${stats.words > 1 ? 's' : ''}`;
                } },
                placeholder: field.placeholder,
                toolbar: {
                    items: ['undo', 'redo', '|', 'heading', '|', 'bold', 'italic', 'underline',
                        '|', 'bulletedList', 'numberedList', 'blockQuote', 'link',
                        '|', 'code', 'codeBlock', 'insertTable', 'lessonPhotoUpload', 'removeFormat'],
                    shouldNotGroupWhenFull: true
                },
                heading: { options: [
                    { model: 'paragraph', title: 'Paragraphe', class: 'ck-heading_paragraph' },
                    { model: 'heading2', view: 'h2', title: 'Titre H2', class: 'ck-heading_heading2' },
                    { model: 'heading3', view: 'h3', title: 'Titre H3', class: 'ck-heading_heading3' },
                    { model: 'heading4', view: 'h4', title: 'Titre H4', class: 'ck-heading_heading4' }
                ] },
                link: { addTargetToExternalLinks: true, defaultProtocol: 'https://' },
                codeBlock: { languages: [{ language: 'plaintext', label: 'Texte brut' }] },
                // Keep tables simple: the server does not allow merged-cell attributes or inline styles.
                table: { contentToolbar: ['tableColumn', 'tableRow'] },
                image: { toolbar: ['imageTextAlternative', 'toggleImageCaption'] },
                // Preserve existing server-allowed markup, including lesson images, without an upload plugin.
                htmlSupport: { allow: [{
                    name: /^(p|h2|h3|h4|strong|em|u|ul|ol|li|blockquote|a|pre|code|br|img|figure|figcaption|table|thead|tbody|tfoot|tr|th|td)$/,
                    attributes: ['href', 'title', 'target', 'rel', 'src', 'alt', 'width', 'height', 'loading'],
                    classes: true
                }] }
            });
            editors.set(field, editor);
            // CKEditor's default italic output is <i>; the existing server allowlist uses <em>.
            editor.conversion.for('dataDowncast').attributeToElement({
                model: 'italic', view: 'em', converterPriority: 'high'
            });
            editor.editing.view.change(writer => {
                writer.setAttribute('aria-label', field.getAttribute('aria-label'), editor.editing.view.document.getRoot());
            });
            // Files reuse the same protected lesson upload endpoint; pasted HTML images stay filtered.
            editor.editing.view.document.on('clipboardInput', (event, data) => {
                if (data.dataTransfer.files.length) {
                    event.stop(); data.preventDefault();
                    if (uploadUrl) uploadPhotos(editor, [...data.dataTransfer.files]);
                    else showPhotoStatus(`Enregistrez ${entityLabel} pour ajouter des photos.`);
                }
            }, { priority: 'highest' });
            editor.plugins.get('ClipboardPipeline').on('inputTransformation', (_, data) => {
                const writer = new CK.UpcastWriter(editor.editing.view.document);
                const removeImages = node => {
                    for (const child of [...node.getChildren()]) {
                        if (child.is('element', 'img')) writer.remove(child);
                        else if (child.is('element')) removeImages(child);
                    }
                };
                removeImages(data.content);
            }, { priority: 'high' });
            const update = () => {
                sync(field, editor);
            };
            editor.model.document.on('change:data', update);
            wrapper.querySelector('[data-editor-error]').hidden = true;
            update();
            // Clicking the original label must focus the rich editor after its textarea is hidden.
            form.querySelector(`label[for="${field.id}"]`)?.addEventListener('click', event => {
                event.preventDefault();
                editor.editing.view.focus();
            });
        } catch (error) {
            editors.delete(field);
            wrapper.querySelector('[data-editor-error]').hidden = false;
            console.error('Lesson editor initialization failed.', error);
        }
    };
    const initializeVisible = () => fields.forEach(initialize);
    form.addEventListener('lesson:tabchange', initializeVisible);
    form.addEventListener('content:tabchange', initializeVisible);
    form.addEventListener('submit', event => {
        if (pendingUploads) {
            event.preventDefault();
            showPhotoStatus('Patientez jusqu’à la fin de l’envoi des photos, puis enregistrez.');
        }
        editors.forEach((editor, field) => { if (editor) sync(field, editor); });
    });
    initializeVisible();
})();
