// Tracks unsaved-changes state and warns on browser refresh / close / back navigation.
window.atlasUnsavedChanges = (function () {
    let isDirty = false;

    function beforeUnload(event) {
        if (!isDirty) return;
        event.preventDefault();
        event.returnValue = '';
    }

    window.addEventListener('beforeunload', beforeUnload);

    return {
        setDirty: function (value) {
            isDirty = !!value;
        }
    };
})();
