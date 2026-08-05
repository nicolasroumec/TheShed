window.copyToClipboard = (text) => navigator.clipboard.writeText(text);

window.downloadFile = (fileName, bytes) => {
    const url = URL.createObjectURL(new Blob([bytes]));
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
