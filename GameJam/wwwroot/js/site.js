// wwwroot/js/site.js

// Global form instance
let form;

class TeamRegistrationForm {
    constructor() {
        this.maxMembers = 4;
        this.members = [this.createEmptyMember()];
        this.selectedFile = null;
        this.allowedExtensions = ['.zip', '.rar']; // Support both ZIP and RAR

        this.init();
    }

    createEmptyMember() {
        return {
            firstName: '',
            lastName: '',
            phoneNumber: ''
        };
    }

    init() {
        this.renderMembers();
        this.setupFileUpload();
        this.setupFormSubmission();
        this.setupAddMemberButton();
    }

    setupAddMemberButton() {
        const addBtn = document.getElementById('btn-add-member');
        if (addBtn) {
            // Remove inline onclick and use addEventListener
            addBtn.onclick = null;
            addBtn.addEventListener('click', () => this.addMember());
        }
    }

    renderMembers() {
        const container = document.getElementById('members-container');
        const countEl = document.getElementById('members-count');
        const addBtn = document.getElementById('btn-add-member');

        if (!container || !countEl) return;

        countEl.textContent = `${this.members.length} از ${this.maxMembers}`;

        if (addBtn) {
            addBtn.disabled = this.members.length >= this.maxMembers;
        }

        // Store reference to this for event handlers
        const self = this;

        container.innerHTML = this.members.map((member, index) => `
            <div class="member-card" data-index="${index}">
                <div class="member-header">
                    <span class="member-number">عضو ${index + 1}</span>
                    ${this.members.length > 1 ? `
                        <button type="button" class="member-remove" data-remove-index="${index}" title="حذف عضو">
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M18 6L6 18M6 6l12 12"/>
                            </svg>
                        </button>
                    ` : ''}
                </div>
                <div class="member-fields">
                    <div class="form-group">
                        <label class="form-label required">نام</label>
                        <input 
                            type="text" 
                            class="form-input member-input" 
                            placeholder="نام"
                            value="${this.escapeHtml(member.firstName)}"
                            data-index="${index}"
                            data-field="firstName"
                            maxlength="50"
                        >
                    </div>
                    <div class="form-group">
                        <label class="form-label required">نام خانوادگی</label>
                        <input 
                            type="text" 
                            class="form-input member-input" 
                            placeholder="نام خانوادگی"
                            value="${this.escapeHtml(member.lastName)}"
                            data-index="${index}"
                            data-field="lastName"
                            maxlength="50"
                        >
                    </div>
                    <div class="form-group">
                        <label class="form-label required">شماره تلفن</label>
                        <input 
                            type="tel" 
                            class="form-input member-input" 
                            placeholder="09123456789"
                            value="${this.escapeHtml(member.phoneNumber)}"
                            data-index="${index}"
                            data-field="phoneNumber"
                            maxlength="11"
                            pattern="09[0-9]{9}"
                        >
                    </div>
                </div>
            </div>
        `).join('');

        // Attach event listeners after rendering
        this.attachMemberEventListeners();
    }

    attachMemberEventListeners() {
        // Handle input changes
        document.querySelectorAll('.member-input').forEach(input => {
            input.addEventListener('input', (e) => {
                const index = parseInt(e.target.dataset.index);
                const field = e.target.dataset.field;
                this.updateMember(index, field, e.target.value);
            });
        });

        // Handle remove buttons
        document.querySelectorAll('.member-remove').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const index = parseInt(e.currentTarget.dataset.removeIndex);
                this.removeMember(index);
            });
        });
    }

    escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    updateMember(index, field, value) {
        if (this.members[index]) {
            this.members[index][field] = value.trim();
        }
    }

    addMember() {
        if (this.members.length < this.maxMembers) {
            this.members.push(this.createEmptyMember());
            this.renderMembers();

            // Focus on the first input of the new member
            setTimeout(() => {
                const newMemberInputs = document.querySelectorAll(`[data-index="${this.members.length - 1}"]`);
                if (newMemberInputs.length > 0) {
                    newMemberInputs[0].focus();
                }
            }, 100);
        }
    }

    removeMember(index) {
        if (this.members.length > 1 && index >= 0 && index < this.members.length) {
            this.members.splice(index, 1);
            this.renderMembers();
        }
    }

    setupFileUpload() {
        const dropArea = document.getElementById('file-drop-area');
        const fileInput = document.getElementById('file-input');

        if (!dropArea || !fileInput) return;

        // Set accept attribute for file input
        fileInput.setAttribute('accept', '.zip,.rar');

        // Click to upload
        dropArea.addEventListener('click', (e) => {
            // Prevent triggering if clicking on remove button
            if (e.target.closest('.file-remove')) return;
            fileInput.click();
        });

        // Drag and drop
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
            });
        });

        ['dragenter', 'dragover'].forEach(eventName => {
            dropArea.addEventListener(eventName, () => {
                dropArea.classList.add('dragover');
            });
        });

        ['dragleave', 'drop'].forEach(eventName => {
            dropArea.addEventListener(eventName, () => {
                dropArea.classList.remove('dragover');
            });
        });

        dropArea.addEventListener('drop', (e) => {
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                this.handleFile(files[0]);
            }
        });

        fileInput.addEventListener('change', (e) => {
            if (e.target.files.length > 0) {
                this.handleFile(e.target.files[0]);
            }
        });

        // Setup file remove button
        const fileRemoveBtn = document.querySelector('.file-remove');
        if (fileRemoveBtn) {
            fileRemoveBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.removeFile();
            });
        }
    }

    isValidArchiveFile(fileName) {
        const lowerFileName = fileName.toLowerCase();
        return this.allowedExtensions.some(ext => lowerFileName.endsWith(ext));
    }

    getFileExtension(fileName) {
        const match = fileName.match(/\.[^.]+$/);
        return match ? match[0].toLowerCase() : '';
    }

    handleFile(file) {
        const dropArea = document.getElementById('file-drop-area');
        const fileInfo = document.getElementById('file-info');
        const fileName = document.getElementById('file-name');
        const fileSize = document.getElementById('file-size');

        // Validate file type
        if (!this.isValidArchiveFile(file.name)) {
            this.showAlert('error', 'فرمت فایل نامعتبر', ['فقط فایل‌های ZIP و RAR مجاز هستند']);
            return;
        }

        // Validate file size (100MB)
        //const maxSize = 100 * 1024 * 1024;
        //if (file.size > maxSize) {
        //    this.showAlert('error', 'حجم فایل زیاد است', ['حداکثر حجم فایل ۱۰۰ مگابایت است']);
        //    return;
        //}

        this.selectedFile = file;
        dropArea.classList.add('has-file');
        fileInfo.style.display = 'flex';

        // Show file extension badge
        const extension = this.getFileExtension(file.name);
        const extensionBadge = extension === '.rar' ?
            '<span class="file-type-badge rar-badge">RAR</span>' :
            '<span class="file-type-badge zip-badge">ZIP</span>';

        fileName.innerHTML = `${this.escapeHtml(file.name)} ${extensionBadge}`;
        fileSize.textContent = this.formatFileSize(file.size);

        // Re-attach remove button listener
        const fileRemoveBtn = document.querySelector('.file-remove');
        if (fileRemoveBtn) {
            fileRemoveBtn.onclick = (e) => {
                e.stopPropagation();
                this.removeFile();
            };
        }
    }

    removeFile() {
        const dropArea = document.getElementById('file-drop-area');
        const fileInfo = document.getElementById('file-info');
        const fileInput = document.getElementById('file-input');

        this.selectedFile = null;
        if (dropArea) dropArea.classList.remove('has-file');
        if (fileInfo) fileInfo.style.display = 'none';
        if (fileInput) fileInput.value = '';
    }

    formatFileSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }

    setupFormSubmission() {
        const formEl = document.getElementById('registration-form');

        if (formEl) {
            formEl.addEventListener('submit', async (e) => {
                e.preventDefault();
                await this.submitForm();
            });
        }

        // Setup modal close button
        const modalCloseBtn = document.querySelector('.btn-modal');
        if (modalCloseBtn) {
            modalCloseBtn.addEventListener('click', () => this.closeModal());
        }
    }

    async submitForm() {
        const submitBtn = document.getElementById('btn-submit');
        const btnText = submitBtn.querySelector('.btn-text');
        const btnSpinner = submitBtn.querySelector('.spinner');

        this.hideAlert();

        const errors = this.validate();
        if (errors.length > 0) {
            this.showAlert('error', 'لطفاً خطاهای زیر را برطرف کنید', errors);
            return;
        }

        submitBtn.disabled = true;
        btnText.textContent = 'در حال ارسال...';
        btnSpinner.style.display = 'block';

        try {
            const teamName = document.getElementById('team-name').value.trim();

            const teamData = {
                teamName: teamName,
                members: this.members
            };

            const formData = new FormData();
            formData.append('teamData', JSON.stringify(teamData));
            formData.append('archiveFile', this.selectedFile);

            // ✅ استفاده از XMLHttpRequest برای نمایش پیشرفت
            const response = await this.uploadWithProgress(formData, (progress) => {
                btnText.textContent = `در حال ارسال... ${progress}%`;
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccessModal(result.data.teamName);
                this.resetForm();
            } else {
                this.showAlert('error', result.message || 'خطا در ثبت‌نام', result.errors || []);
            }
        } catch (error) {
            console.error('Submission error:', error);
            this.showAlert('error', 'خطا در ارتباط با سرور', ['لطفاً دوباره تلاش کنید']);
        } finally {
            submitBtn.disabled = false;
            btnText.textContent = 'ثبت‌نام تیم';
            btnSpinner.style.display = 'none';
        }
    }

    uploadWithProgress(formData, onProgress) {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();

            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable) {
                    const percentComplete = Math.round((e.loaded / e.total) * 100);
                    onProgress(percentComplete);
                }
            });

            xhr.addEventListener('load', () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve({
                        json: () => Promise.resolve(JSON.parse(xhr.responseText)),
                        status: xhr.status
                    });
                } else {
                    reject(new Error(`HTTP ${xhr.status}`));
                }
            });

            xhr.addEventListener('error', () => reject(new Error('Network error')));
            xhr.addEventListener('abort', () => reject(new Error('Upload aborted')));

            xhr.open('POST', '/api/registration/submit');
            xhr.send(formData);
        });
    }

    validate() {
        const errors = [];
        const teamName = document.getElementById('team-name').value.trim();

        // Team name validation
        if (!teamName) {
            errors.push('نام تیم الزامی است');
        } else if (teamName.length < 3) {
            errors.push('نام تیم باید حداقل ۳ کاراکتر باشد');
        } else if (teamName.length > 100) {
            errors.push('نام تیم نباید بیشتر از ۱۰۰ کاراکتر باشد');
        }

        // Members validation
        const phoneRegex = /^09\d{9}$/;
        const phoneNumbers = new Set();

        this.members.forEach((member, index) => {
            const num = index + 1;

            if (!member.firstName) {
                errors.push(`عضو ${num}: نام الزامی است`);
            } else if (member.firstName.length < 2) {
                errors.push(`عضو ${num}: نام باید حداقل ۲ کاراکتر باشد`);
            }

            if (!member.lastName) {
                errors.push(`عضو ${num}: نام خانوادگی الزامی است`);
            } else if (member.lastName.length < 2) {
                errors.push(`عضو ${num}: نام خانوادگی باید حداقل ۲ کاراکتر باشد`);
            }

            if (!member.phoneNumber) {
                errors.push(`عضو ${num}: شماره تلفن الزامی است`);
            } else if (!phoneRegex.test(member.phoneNumber)) {
                errors.push(`عضو ${num}: فرمت شماره تلفن نامعتبر است (مثال: 09123456789)`);
            } else if (phoneNumbers.has(member.phoneNumber)) {
                errors.push(`عضو ${num}: شماره تلفن تکراری است`);
            } else {
                phoneNumbers.add(member.phoneNumber);
            }
        });

        // File validation
        if (!this.selectedFile) {
            errors.push('آپلود فایل فشرده (ZIP یا RAR) الزامی است');
        } else if (!this.isValidArchiveFile(this.selectedFile.name)) {
            errors.push('فقط فایل‌های ZIP و RAR مجاز هستند');
        }

        return errors;
    }

    showAlert(type, title, messages) {
        const alertContainer = document.getElementById('alert-container');
        if (!alertContainer) return;

        const isError = type === 'error';

        alertContainer.innerHTML = `
            <div class="alert alert-${type}">
                <span class="alert-icon">
                    ${isError ? `
                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <circle cx="12" cy="12" r="10"/>
                            <path d="M12 8v4M12 16h.01"/>
                        </svg>
                    ` : `
                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                            <polyline points="22 4 12 14.01 9 11.01"/>
                        </svg>
                    `}
                </span>
                <div class="alert-content">
                    <div class="alert-title">${this.escapeHtml(title)}</div>
                    ${messages.length > 0 ? `
                        <ul class="alert-list">
                            ${messages.map(msg => `<li>${this.escapeHtml(msg)}</li>`).join('')}
                        </ul>
                    ` : ''}
                </div>
            </div>
        `;

        // Scroll to alert
        alertContainer.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    hideAlert() {
        const alertContainer = document.getElementById('alert-container');
        if (alertContainer) {
            alertContainer.innerHTML = '';
        }
    }

    showSuccessModal(teamName) {
        const modal = document.getElementById('success-modal');
        const teamNameEl = document.getElementById('modal-team-name');

        if (teamNameEl) teamNameEl.textContent = teamName;
        if (modal) modal.classList.add('active');
    }

    closeModal() {
        const modal = document.getElementById('success-modal');
        if (modal) modal.classList.remove('active');
    }

    resetForm() {
        const teamNameInput = document.getElementById('team-name');
        if (teamNameInput) teamNameInput.value = '';

        this.members = [this.createEmptyMember()];
        this.renderMembers();
        this.removeFile();
        this.hideAlert();
    }
}

// Initialize form when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    form = new TeamRegistrationForm();
});

// Also try to initialize if DOM is already loaded
if (document.readyState === 'complete' || document.readyState === 'interactive') {
    setTimeout(() => {
        if (!form) {
            form = new TeamRegistrationForm();
        }
    }, 1);
}