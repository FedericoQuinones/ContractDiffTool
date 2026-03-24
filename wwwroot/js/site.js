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
        if (!compareBtn) return;
        const ready = originalInput.files.length > 0 && revisedInput.files.length > 0;
        compareBtn.disabled = !ready;
        if (ready) {
            compareBtn.style.animation = 'subtle-pulse 0.4s ease';
            setTimeout(() => compareBtn.style.animation = '', 400);
        }
    }

    function formatFileSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / 1048576).toFixed(1) + ' MB';
    }

    function handleFileSelect(input, nameEl, dropZone) {
        if (input.files.length > 0) {
            var file = input.files[0];
            nameEl.textContent = file.name + ' (' + formatFileSize(file.size) + ')';
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

    // Drag & drop
    function setupDropZone(zone, input, nameEl) {
        ['dragenter', 'dragover'].forEach(function(evt) {
            zone.addEventListener(evt, function (e) {
                e.preventDefault();
                e.stopPropagation();
                zone.classList.add('drag-over');
            });
        });
        ['dragleave', 'drop'].forEach(function(evt) {
            zone.addEventListener(evt, function (e) {
                e.preventDefault();
                e.stopPropagation();
                zone.classList.remove('drag-over');
            });
        });
        zone.addEventListener('drop', function (e) {
            if (e.dataTransfer.files.length > 0) {
                input.files = e.dataTransfer.files;
                handleFileSelect(input, nameEl, zone);
            }
        });
    }

    if (dropOriginal) setupDropZone(dropOriginal, originalInput, originalName);
    if (dropRevised) setupDropZone(dropRevised, revisedInput, revisedName);

    // Loading state
    if (form) {
        form.addEventListener('submit', function () {
            if (compareBtn && !compareBtn.disabled) {
                var btnText = compareBtn.querySelector('.btn-text');
                var btnLoading = compareBtn.querySelector('.btn-loading');
                if (btnText) btnText.style.display = 'none';
                if (btnLoading) {
                    btnLoading.style.display = 'inline-flex';
                    btnLoading.style.alignItems = 'center';
                    btnLoading.style.gap = '0.5rem';
                }
                compareBtn.disabled = true;
                compareBtn.style.opacity = '0.7';
            }
        });
    }
});
