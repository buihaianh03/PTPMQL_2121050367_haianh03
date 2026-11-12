(() => {
    function initializeStudentForm() {
    const form = document.getElementById("student-form");
    const button = document.getElementById("preview-code");
    const code = document.getElementById("MaSV");
    const message = document.getElementById("code-message");

    if (!form || !button || !code || !message || form.dataset.previewInitialized === "true") {
        return;
    }
    form.dataset.previewInitialized = "true";

    const fields = ["AdmissionYear", "TrainingSystemId", "MajorId"];
    const initialValues = Object.fromEntries(
        fields.map((name) => [name, document.getElementById(name).value])
    );
    const isEdit = form.dataset.isEdit === "true";
    const originalCode = code.dataset.originalCode || "";

    for (const name of fields) {
        document.getElementById(name).addEventListener("change", () => {
            const unchanged = isEdit && fields.every(
                (field) => document.getElementById(field).value === initialValues[field]
            );
            code.value = unchanged ? originalCode : "";
            message.textContent = unchanged
                ? "Giữ nguyên mã hiện tại."
                : "Thông tin năm, hệ hoặc ngành đã đổi. Hãy tạo mã mới trước khi lưu.";
        });
    }

    button.addEventListener("click", async () => {
        const query = new URLSearchParams({
            admissionYear: document.getElementById("AdmissionYear").value,
            trainingSystemId: document.getElementById("TrainingSystemId").value,
            majorId: document.getElementById("MajorId").value
        });

        button.disabled = true;
        message.textContent = "Đang kiểm tra mã...";
        try {
            const response = await fetch(`${form.dataset.previewUrl}?${query}`);
            const result = await response.json();
            if (!response.ok) {
                code.value = "";
                message.textContent = result.message || "Không thể tạo mã với dữ liệu hiện tại.";
                return;
            }

            code.value = result.code;
            message.textContent = "Mã dự kiến. Khi lưu, hệ thống sẽ kiểm tra lại để tránh trùng do thao tác đồng thời.";
        } catch {
            code.value = "";
            message.textContent = "Không kết nối được máy chủ để tạo mã. Vui lòng thử lại.";
        } finally {
            button.disabled = false;
        }
    });
    }

    window.initializeStudentForm = initializeStudentForm;
    initializeStudentForm();
})();
