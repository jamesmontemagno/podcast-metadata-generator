// Clipboard operations
window.copyToClipboard = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error('Failed to copy to clipboard:', err);
        // Fallback for older browsers
        try {
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            textArea.style.top = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            document.execCommand('copy');
            textArea.remove();
            return true;
        } catch (fallbackErr) {
            console.error('Fallback copy failed:', fallbackErr);
            return false;
        }
    }
};

// File download
window.downloadFile = function (filename, content, contentType) {
    const blob = new Blob([content], { type: contentType || 'text/plain' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
};

// Download text file helper
window.downloadTextFile = function (filename, content) {
    window.downloadFile(filename, content, 'text/plain;charset=utf-8');
};

// Download multiple files as zip (requires JSZip library)
window.downloadAsZip = async function (filename, files) {
    // files is an array of { name: string, content: string }
    // For simplicity, we'll create individual downloads
    // In production, include JSZip library
    for (const file of files) {
        window.downloadFile(file.name, file.content);
        await new Promise(resolve => setTimeout(resolve, 100));
    }
};

// Scroll to bottom of element
window.scrollToBottom = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};
