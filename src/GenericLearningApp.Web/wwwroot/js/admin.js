// Small browser helpers for the admin area: clipboard, file download, and a "you have unpublished
// changes" prompt when the tab is closed.
(function () {
  window.glaCopy = async function (text) {
    try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
  };

  window.glaDownload = function (filename, text) {
    const url = URL.createObjectURL(new Blob([text], { type: "application/json;charset=utf-8" }));
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  };

  let dirty = false;
  window.addEventListener("beforeunload", function (e) {
    if (!dirty) return;
    e.preventDefault();
    e.returnValue = "";
  });
  window.glaAdminDirty = function (value) { dirty = !!value; };
})();
