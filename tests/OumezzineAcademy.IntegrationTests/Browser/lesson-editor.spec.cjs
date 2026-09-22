const { test, expect } = require('@playwright/test');

const editable = (page, language = 'fr') => page.locator(`#lesson-${language} .ck-editor__main > .ck-editor__editable`);
const field = (page, prefix = 'French') => page.locator(`#${prefix}-ContentHtml`);
let server;
test.beforeAll(async () => {
    ({ server } = await import('./server.mjs'));
    await new Promise(resolve => server.listen(4197, '127.0.0.1', resolve));
});
test.afterAll(async () => { await new Promise(resolve => server.close(resolve)); });

test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
        window.cspViolations = [];
        window.addEventListener('securitypolicyviolation', event => window.cspViolations.push(`${event.violatedDirective}: ${event.blockedURI}`));
    });
});

test('create: separate languages, live preview, word count, submit, tabs and undo', async ({ page }) => {
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    await page.goto('/create');
    await expect(editable(page)).toBeVisible();
    await expect(page.locator('[data-editor-error]:visible')).toHaveCount(0);
    await expect(editable(page).locator('[data-placeholder]')).toHaveAttribute('data-placeholder', 'Commencez à rédiger le contenu de la leçon…');
    await editable(page).fill('Bonjour le monde');
    await expect(field(page)).toHaveValue('<p>Bonjour le monde</p>');
    await expect(page.locator('[data-preview-html]')).toHaveText('Bonjour le monde');
    await expect(page.locator('[data-word-count="French"]')).toHaveText('3 mots');
    await editable(page).press('ControlOrMeta+a');
    await editable(page).press('ControlOrMeta+b');
    await expect(field(page)).toHaveValue('<p><strong>Bonjour le monde</strong></p>');
    await editable(page).press('ControlOrMeta+z');
    await expect(field(page)).toHaveValue('<p>Bonjour le monde</p>');
    await editable(page).press('ControlOrMeta+Shift+z');
    await expect(field(page)).toHaveValue('<p><strong>Bonjour le monde</strong></p>');
    await page.locator('[data-lesson-tab="lesson-fr"]').focus();
    await page.keyboard.press('ArrowRight');
    await expect(editable(page, 'en')).toBeVisible();
    await expect(editable(page, 'en')).toBeEmpty();
    await editable(page, 'en').fill('Independent English content');
    await expect(field(page, 'English')).toHaveValue('<p>Independent English content</p>');
    await page.locator('[data-preview-language="en"]').click();
    await expect(page.locator('[data-preview-html]')).toHaveText('Independent English content');
    for (let i = 0; i < 3; i++) {
        await page.locator('[data-lesson-tab="lesson-fr"]').click();
        await page.locator('[data-lesson-tab="lesson-en"]').click();
    }
    await expect(page.locator('.ck-editor')).toHaveCount(2);
    const values = await page.evaluate(() => {
        const form = document.querySelector('.lesson-concept-form');
        form.addEventListener('submit', event => event.preventDefault(), { once: true });
        form.requestSubmit();
        return Object.fromEntries(new FormData(form));
    });
    expect(values['French.ContentHtml']).toBe('<p><strong>Bonjour le monde</strong></p>');
    expect(values['English.ContentHtml']).toBe('<p>Independent English content</p>');
    expect(await page.evaluate(() => window.cspViolations)).toEqual([]);
    expect(errors).toEqual([]);
});

test('edit loads allowed markup, toolbar changes it and preview follows the textarea', async ({ page }) => {
    await page.goto('/edit');
    await expect(editable(page)).toBeVisible();
    for (const selector of ['h2', 'strong', 'i,em', 'u', 'ul', 'blockquote', 'pre code', 'a', 'img', 'figcaption', 'table']) {
        await expect(editable(page).locator(selector).first()).toBeVisible();
    }
    await expect(field(page)).toHaveValue(/rel="noopener noreferrer"/);
    await expect(field(page)).toHaveValue(/alt="Existant"/);
    await expect(field(page)).toHaveValue(/<em>élève<\/em>/);
    await editable(page).fill('Texte modifié');
    await editable(page).press('ControlOrMeta+a');
    await editable(page).press('ControlOrMeta+i');
    await editable(page).press('ControlOrMeta+u');
    await expect(field(page)).toHaveValue(/<em>/);
    await expect(field(page)).toHaveValue(/<u>/);
    await expect(page.locator('[data-preview-html]')).toHaveText('Texte modifié');
    await page.locator('[data-lesson-tab="lesson-en"]').click();
    await expect(editable(page, 'en')).toContainText('English lesson');
    expect(await page.evaluate(() => window.cspViolations)).toEqual([]);
});

test('validation redisplay and failed script retain posted content', async ({ page }) => {
    await page.route('**/ckeditor5.umd.js*', route => route.abort());
    await page.goto('/validation');
    await expect(field(page)).toBeVisible();
    await expect(field(page)).toHaveValue('<p>Unsaved correction</p>');
    await expect(page.locator('#lesson-fr [data-editor-error]')).toBeVisible();
    await page.locator('[data-lesson-tab="lesson-en"]').click();
    await expect(field(page, 'English')).toBeVisible();
    await expect(field(page, 'English')).toHaveValue(/Independent/);
});

test('paste keeps text formatting but rejects embedded images and active HTML', async ({ page }) => {
    await page.goto('/create');
    await expect(editable(page)).toBeVisible();
    await editable(page).focus();
    await editable(page).evaluate(element => {
        const clipboardData = new DataTransfer();
        clipboardData.setData('text/html', '<p><strong>Collé</strong> <em>texte</em></p><img src="data:image/png;base64,AAAA"><img src="/favicon.ico"><script>alert(1)</script>');
        element.dispatchEvent(new ClipboardEvent('paste', { clipboardData, bubbles: true, cancelable: true }));
    });
    await expect(field(page)).toHaveValue(/<strong>Collé<\/strong>/);
    await expect(field(page)).toHaveValue(/<em>texte<\/em>/);
    expect(await field(page).inputValue()).not.toMatch(/<img|data:|<script/);
    expect(await page.evaluate(() => window.cspViolations)).toEqual([]);
});

const photo = name => ({ name, mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a9HkAAAAASUVORK5CYII=', 'base64') });
const choosePhotos = async (page, files, language = 'fr') => {
    const chooser = page.waitForEvent('filechooser');
    await page.locator(`#lesson-${language}`).getByRole('button', { name: 'Insérer une photo', exact: true }).click();
    await (await chooser).setFiles(files);
};

test('multiple photos upload with antiforgery, block saving while pending and stay in their language', async ({ page }) => {
    await page.goto('/edit');
    await expect(editable(page)).toBeVisible();
    await editable(page).fill('Texte français');
    const id = await page.locator('input[name="Id"]').inputValue();
    const token = await page.locator('.lesson-concept-form input[name="__RequestVerificationToken"]').inputValue();
    let release;
    const gate = new Promise(resolve => release = resolve);
    let count = 0;
    await page.route('**/image/*', async route => {
        expect(route.request().method()).toBe('POST');
        expect(route.request().postDataBuffer().toString()).toContain(token);
        expect(route.request().postDataBuffer().toString()).toContain('name="file"');
        await gate;
        await route.fulfill({ json: { url: `/uploads/lessons/${id}/photo-${++count}.png`, alt: `Photo ${count}` } });
    });
    await page.route('**/uploads/lessons/**', route => route.fulfill({ contentType: 'image/png', body: photo('image.png').buffer }));
    await choosePhotos(page, [photo('premiere.png'), photo('deuxieme.png')]);
    await expect(page.locator('[data-photo-status]')).toContainText('Envoi de premiere.png');
    await expect(page.locator('.lesson-concept-form button[type="submit"]')).toBeDisabled();
    expect(await field(page).inputValue()).not.toContain('<img');
    release();
    await expect(editable(page).locator('img')).toHaveCount(2);
    await expect(page.locator('[data-preview-html] img')).toHaveCount(2);
    await expect(page.locator('.lesson-concept-form button[type="submit"]')).toBeEnabled();
    expect(await field(page).inputValue()).not.toMatch(/blob:|data:image/);
    const french = await field(page).inputValue();
    await page.locator('[data-lesson-tab="lesson-en"]').click();
    await expect(editable(page, 'en')).toBeVisible();
    await editable(page, 'en').fill('English photo');
    await choosePhotos(page, [photo('english.png')], 'en');
    await expect(editable(page, 'en').locator('img')).toHaveCount(1);
    await expect(field(page)).toHaveValue(french);
    expect(await page.evaluate(() => window.cspViolations)).toEqual([]);
});

test('photo upload failure preserves text and permits a retry; Create explains the first save', async ({ page }) => {
    await page.goto('/create');
    await expect(editable(page)).toBeVisible();
    await expect(page.locator('#lesson-fr').getByRole('button', { name: 'Insérer une photo', exact: true })).toBeDisabled();
    await expect(page.locator('.lesson-photo-help')).toContainText('Enregistrez');
    await page.goto('/edit');
    await expect(editable(page)).toBeVisible();
    await editable(page).fill('Texte à conserver');
    await page.route('**/image/*', route => route.fulfill({ status: 400, json: { title: 'Invalid image' } }));
    await choosePhotos(page, [photo('rejected.png')]);
    await expect(page.locator('[data-photo-status]')).toContainText('n’a pas pu être envoyée');
    await expect(field(page)).toHaveValue('<p>Texte à conserver</p>');
    await expect(page.locator('.lesson-concept-form button[type="submit"]')).toBeEnabled();
    await expect(page.locator('#lesson-fr').getByRole('button', { name: 'Insérer une photo', exact: true })).toBeEnabled();
    await expect(editable(page)).toHaveAttribute('contenteditable', 'true');
});

test('heading, lists, links and remove formatting work from the toolbar', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 1000 });
    await page.goto('/create');
    await expect(editable(page)).toBeVisible();
    await editable(page).fill('Exemple');
    await page.locator('#lesson-fr .ck-heading-dropdown > button').click();
    await page.getByText('Titre H2', { exact: true }).click();
    await expect(field(page)).toHaveValue('<h2>Exemple</h2>');
    await page.locator('#lesson-fr .ck-heading-dropdown > button').click();
    await page.getByText('Paragraphe', { exact: true }).last().click();
    const toolbar = page.locator('#lesson-fr .ck-toolbar').first();
    await toolbar.getByRole('button', { name: 'Liste à puces', exact: true }).click();
    await expect(field(page)).toHaveValue(/<ul><li[^>]*>Exemple<\/li><\/ul>/);
    await toolbar.getByRole('button', { name: 'Liste numérotée', exact: true }).click();
    await expect(field(page)).toHaveValue(/<ol><li[^>]*>Exemple<\/li><\/ol>/);
    await toolbar.getByRole('button', { name: 'Liste numérotée', exact: true }).click();
    await editable(page).press('ControlOrMeta+a');
    await toolbar.getByRole('button', { name: 'Gras', exact: true }).click();
    await expect(field(page)).toHaveValue(/<strong>/);
    await toolbar.getByRole('button', { name: 'Enlever le format', exact: true }).click();
    await expect(field(page)).toHaveValue('<p>Exemple</p>');
    await editable(page).press('ControlOrMeta+a');
    await editable(page).press('ControlOrMeta+k');
    await page.getByRole('textbox', { name: 'URL du lien', exact: true }).fill('https://example.com/course');
    const popup = page.locator('.ck-balloon-panel_visible:not(.ck-powered-by-balloon)');
    const bounds = await popup.boundingBox();
    expect(bounds.x).toBeGreaterThanOrEqual(0);
    expect(bounds.x + bounds.width).toBeLessThanOrEqual(375);
    await popup.getByRole('button', { name: 'Insérer', exact: true }).click();
    await expect(field(page)).toHaveValue(/href="https:\/\/example.com\/course"/);
    await expect(field(page)).toHaveValue(/rel="noopener noreferrer"/);
    await expect(field(page)).toHaveValue(/target="_blank"/);
    expect(await page.evaluate(() => window.cspViolations)).toEqual([]);
});

for (const width of [375, 768, 1440]) {
    test(`compact editor and wrapping toolbar at ${width}px under strict CSP`, async ({ page }) => {
        await page.setViewportSize({ width, height: 1000 });
        await page.goto('/create');
        await expect(editable(page)).toBeVisible();
        const bounds = await editable(page).boundingBox();
        expect(bounds.height).toBeGreaterThanOrEqual(240);
        expect(bounds.height).toBeLessThanOrEqual(280);
        const overflow = () => page.evaluate(() => document.documentElement.scrollWidth > innerWidth);
        expect(await overflow()).toBe(false);
        await editable(page).fill('Unmottrèslong'.repeat(150));
        expect(await overflow()).toBe(false);
        expect((await editable(page).boundingBox()).height).toBeLessThanOrEqual(620);
        await page.locator('[data-lesson-tab="lesson-en"]').click();
        await expect(editable(page, 'en')).toBeVisible();
        expect((await editable(page, 'en').boundingBox()).height).toBeGreaterThanOrEqual(240);
        expect(await overflow()).toBe(false);
        expect(await page.evaluate(() => window.cspViolations)).toEqual([]);
        if (width === 375) await page.screenshot({ path: 'obj/mobile-editor.png', fullPage: true });
    });
}