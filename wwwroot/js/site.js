document.addEventListener('DOMContentLoaded', function () {
    const originalInput = document.getElementById('originalFile');
    const revisedInput = document.getElementById('revisedFile');
    const originalName = document.getElementById('originalName');
    const revisedName = document.getElementById('revisedName');
    const compareBtn = document.getElementById('compareBtn');
    const form = document.getElementById('uploadForm');
    const dropOriginal = document.getElementById('dropOriginal');
    const dropRevised = document.getElementById('dropRevised');

    if (!originalInput || !revisedInput) return;

    function updateButton() {
        if (compareBtn) {
            compareBtn.disabled = !(originalInput.files.length > 0 && revisedInput.files.length > 0);
        }
    }

    function handleFileSelect(input, nameEl, dropZone) {
        if (input.files.length > 0) {
            nameEl.textContent = input.files[0].name;
            dropZone.classList.add('has-file');
        } else {
            nameEl.textContent = '';
            dropZone.classList.remove('has-file');
        }
        updateButton();
    }

    originalInput.addEventListener('change', function () {
        handleFileSelect(originalInput, originalName, dropOriginal);
    });

    revisedInput.addEventListener('change', function () {
        handleFileSelect(revisedInput, revisedName, dropRevised);
    });

    // Drag & drop support
    function setupDropZone(zone, input, nameEl) {
        zone.addEventListener('dragover', function (e) {
            e.preventDefault();
            zone.classList.add('drag-over');
        });
        zone.addEventListener('dragleave', function () {
            zone.classList.remove('drag-over');
        });
        zone.addEventListener('drop', function (e) {
            e.preventDefault();
            zone.classList.remove('drag-over');
            if (e.dataTransfer.files.length > 0) {
                input.files = e.dataTransfer.files;
                handleFileSelect(input, nameEl, zone);
            }
        });
    }

    if (dropOriginal) setupDropZone(dropOriginal, originalInput, originalName);
    if (dropRevised) setupDropZone(dropRevised, revisedInput, revisedName);

    // Loading state on submit
    if (form) {
        form.addEventListener('submit', function () {
            if (compareBtn && !compareBtn.disabled) {
                var btnText = compareBtn.querySelector('.btn-text');
                var btnLoading = compareBtn.querySelector('.btn-loading');
                if (btnText) btnText.style.display = 'none';
                if (btnLoading) btnLoading.style.display = 'inline-flex';
                compareBtn.disabled = true;
            }
        });
    }
});
