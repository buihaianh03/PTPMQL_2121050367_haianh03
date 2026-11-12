(() => {
    const modalElement = document.getElementById("ajaxCrudModal");
    const modalBody = document.getElementById("ajaxCrudBody");
    const modalTitle = document.getElementById("ajaxCrudTitle");
    const notificationContainer = document.getElementById("ajaxCrudNotifications");

    if (!modalElement || !modalBody || !modalTitle || !notificationContainer) {
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

    function showNotification(message, isError = false) {
        const notification = document.createElement("div");
        notification.className = `alert ${isError ? "alert-danger" : "alert-success"} alert-dismissible shadow`;
        notification.setAttribute("role", "alert");
        notification.textContent = message;

        const closeButton = document.createElement("button");
        closeButton.type = "button";
        closeButton.className = "btn-close";
        closeButton.setAttribute("aria-label", "Đóng");
        closeButton.addEventListener("click", () => notification.remove());
        notification.append(closeButton);
        notificationContainer.append(notification);
        window.setTimeout(() => notification.remove(), 6000);
    }

    function showLoading(message) {
        modalBody.innerHTML = "";
        const status = document.createElement("div");
        status.className = "d-flex align-items-center gap-2";
        status.setAttribute("role", "status");
        status.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';
        const label = document.createElement("span");
        label.textContent = message;
        status.append(label);
        modalBody.append(status);
    }

    async function readError(response) {
        const contentType = response.headers.get("content-type") || "";
        if (contentType.includes("application/json")) {
            const result = await response.json();
            return result.message || "Đã xảy ra lỗi khi xử lý yêu cầu.";
        }

        return await response.text() || `Yêu cầu thất bại (HTTP ${response.status}).`;
    }

    async function openModal(url, title) {
        if (modalElement.dataset.loading === "true") {
            return;
        }

        modalElement.dataset.loading = "true";
        modalTitle.textContent = title;
        showLoading("Đang tải dữ liệu...");
        modal.show();

        try {
            const response = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            if (!response.ok) {
                throw new Error(await readError(response));
            }

            modalBody.innerHTML = await response.text();
            window.initializeStudentForm?.();
        } catch (error) {
            modalBody.textContent = error.message || "Không thể tải dữ liệu. Vui lòng thử lại.";
            showNotification(error.message || "Không thể tải dữ liệu.", true);
        } finally {
            delete modalElement.dataset.loading;
        }
    }

    async function refreshRows() {
        const currentRows = document.querySelector("tbody[data-ajax-rows]");
        if (!currentRows) {
            return;
        }

        const url = new URL(window.location.href);
        url.searchParams.set("partial", "rows");
        const response = await fetch(url, {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });
        if (!response.ok) {
            throw new Error(await readError(response));
        }

        const template = document.createElement("template");
        template.innerHTML = (await response.text()).trim();
        const updatedRows = template.content.querySelector("tbody[data-ajax-rows]");
        if (!updatedRows) {
            throw new Error("Máy chủ không trả về danh sách dữ liệu hợp lệ.");
        }

        currentRows.replaceWith(updatedRows);
    }

    document.addEventListener("click", (event) => {
        const cancel = event.target.closest("[data-modal-cancel]");
        if (cancel && modalElement.contains(cancel)) {
            event.preventDefault();
            modal.hide();
            return;
        }

        const trigger = event.target.closest("[data-ajax-modal]");
        if (!trigger) {
            return;
        }

        event.preventDefault();
        openModal(trigger.href, trigger.dataset.modalTitle || trigger.textContent.trim());
    });

    document.addEventListener("submit", async (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !modalElement.contains(form)) {
            return;
        }

        event.preventDefault();
        if (form.dataset.ajaxSubmitting === "true" || !form.reportValidity()) {
            return;
        }

        form.dataset.ajaxSubmitting = "true";
        const submitButtons = Array.from(form.querySelectorAll('button[type="submit"], input[type="submit"]'));
        submitButtons.forEach((button) => {
            button.disabled = true;
        });
        const previousBody = modalBody.innerHTML;
        showLoading("Đang xử lý...");

        try {
            const response = await fetch(form.action, {
                method: (form.method || "POST").toUpperCase(),
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    "Accept": "application/json"
                },
                body: new FormData(form)
            });
            const contentType = response.headers.get("content-type") || "";

            if (response.ok && contentType.includes("application/json")) {
                const result = await response.json();
                modal.hide();
                showNotification(result.message || "Đã lưu dữ liệu thành công.");
                try {
                    await refreshRows();
                } catch (error) {
                    showNotification(`Đã lưu dữ liệu nhưng không thể cập nhật danh sách: ${error.message}`, true);
                }
                return;
            }

            if (contentType.includes("text/html")) {
                modalBody.innerHTML = await response.text();
                window.initializeStudentForm?.();
                if (!response.ok && response.status !== 422) {
                    showNotification("Không thể xử lý yêu cầu. Vui lòng kiểm tra lại.", true);
                }
                return;
            }

            throw new Error(await readError(response));
        } catch (error) {
            modalBody.innerHTML = previousBody;
            showNotification(error.message || "Không thể kết nối máy chủ. Vui lòng thử lại.", true);
        } finally {
            delete form.dataset.ajaxSubmitting;
            submitButtons.forEach((button) => {
                button.disabled = false;
            });
        }
    });
})();
