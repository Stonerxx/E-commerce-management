(function () {
    const { createApp } = Vue;
    const toLocalInput = value => {
        const date = new Date(value);
        const offset = date.getTimezoneOffset() * 60000;
        return new Date(date.getTime() - offset).toISOString().slice(0, 16);
    };
    const defaultForm = () => {
        const now = new Date();
        const end = new Date(now.getTime() + 30 * 86400000);
        return { name: '', type: 1, amount: 10, minAmount: 100, totalCount: 100, startTime: toLocalInput(now), endTime: toLocalInput(end), status: 1, applicableCategoryId: null };
    };

    createApp({
        data() {
            return { loading: false, saving: false, showForm: false, editingId: null, form: defaultForm(), items: [], categories: [], pageIndex: 1, pageSize: 10, totalCount: 0, totalPages: 1, filters: { keyword: '', status: '' } };
        },
        mounted() { this.load(); this.loadCategories(); },
        methods: {
            async loadCategories() {
                try {
                    const response = await fetch('/api/v1/admin/categories', { headers: { Accept: 'application/json' } });
                    const result = await response.json();
                    if (!response.ok || !result.success) throw new Error(result.message || '品类加载失败');
                    const leaves = [];
                    const visit = (nodes, parents = []) => (nodes || []).forEach(node => {
                        if (node.status === 1 && (node.children || []).length === 0) {
                            leaves.push({ categoryId: node.categoryId, label: [...parents, node.name].join(' / ') });
                        }
                        visit(node.children, [...parents, node.name]);
                    });
                    visit(result.data);
                    this.categories = leaves;
                } catch (error) {
                    alert(error.message || '品类加载失败');
                }
            },
            async load() {
                this.loading = true;
                const params = new URLSearchParams({ pageIndex: this.pageIndex, pageSize: this.pageSize });
                if (this.filters.keyword) params.set('keyword', this.filters.keyword);
                if (this.filters.status !== '') params.set('status', this.filters.status);
                try {
                    const response = await fetch(`/api/v1/admin/coupon-templates?${params}`, { headers: { Accept: 'application/json' } });
                    const result = await response.json();
                    if (!response.ok || !result.success) throw new Error(result.message || '加载失败');
                    this.items = result.data.items || [];
                    this.totalCount = result.data.totalCount || 0;
                    this.totalPages = Math.max(1, result.data.totalPages || 1);
                } catch (error) { alert(error.message || '加载失败'); } finally { this.loading = false; }
            },
            search() { this.pageIndex = 1; this.load(); },
            goPage(page) { if (page < 1 || page > this.totalPages) return; this.pageIndex = page; this.load(); },
            openCreate() { this.editingId = null; this.form = defaultForm(); this.showForm = true; },
            openEdit(item) { this.editingId = item.templateId; this.form = { ...item, startTime: toLocalInput(item.startTime), endTime: toLocalInput(item.endTime) }; this.showForm = true; window.scrollTo({ top: 0, behavior: 'smooth' }); },
            async save() {
                if (!this.form.name || !this.form.startTime || !this.form.endTime) { alert('请填写完整模板信息'); return; }
                this.saving = true;
                const payload = { ...this.form, applicableCategoryId: this.form.applicableCategoryId || null, startTime: new Date(this.form.startTime).toISOString(), endTime: new Date(this.form.endTime).toISOString() };
                try {
                    const url = this.editingId ? `/api/v1/admin/coupon-templates/${this.editingId}` : '/api/v1/admin/coupon-templates';
                    const response = await fetch(url, { method: this.editingId ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json', Accept: 'application/json' }, body: JSON.stringify(payload) });
                    const result = await response.json();
                    if (!response.ok || !result.success) throw new Error(result.message || '保存失败');
                    this.showForm = false; await this.load();
                } catch (error) { alert(error.message || '保存失败'); } finally { this.saving = false; }
            },
            async toggleStatus(item) {
                const response = await fetch(`/api/v1/admin/coupon-templates/${item.templateId}/status`, { method: 'PUT', headers: { 'Content-Type': 'application/json', Accept: 'application/json' }, body: JSON.stringify({ status: item.status === 1 ? 0 : 1 }) });
                const result = await response.json();
                if (!response.ok || !result.success) { alert(result.message || '状态更新失败'); return; }
                await this.load();
            },
            ruleText(item) { return item.type === 1 ? `满 ¥${item.minAmount.toFixed(2)} 减 ¥${item.amount.toFixed(2)}` : `满 ¥${item.minAmount.toFixed(2)} 享 ${(item.amount * 10).toFixed(1)} 折`; },
            formatDate(value) { return value ? new Date(value).toLocaleString('zh-CN') : '-'; }
        }
    }).mount('#adminCouponsApp');
})();
