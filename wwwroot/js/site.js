// Small, dependency-free enhancements.
(function () {
    "use strict";

    // Auto-dismiss success/info alerts after a few seconds.
    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll(".alert-success, .alert-info").forEach(function (el) {
            setTimeout(function () {
                if (window.bootstrap && bootstrap.Alert) {
                    bootstrap.Alert.getOrCreateInstance(el).close();
                }
            }, 6000);
        });

        // Show the chosen file name next to custom file inputs.
        document.querySelectorAll("input[type=file][data-filename-target]").forEach(function (input) {
            input.addEventListener("change", function () {
                var target = document.querySelector(input.getAttribute("data-filename-target"));
                if (target) {
                    target.textContent = input.files && input.files.length ? input.files[0].name : "";
                }
            });
        });
    });
})();
