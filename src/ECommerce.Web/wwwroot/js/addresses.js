(function () {
    const { createApp } = Vue;
    const emptyForm = () => ({ addressId: null, receiverName: "", receiverPhone: "", province: "", city: "", district: "", detailAddress: "", isDefault: false });

    createApp({
        data() {
            return { loading: true, saving: false, error: "", items: [], form: emptyForm() };
        },
        mounted() {
            this.loadAddresses();
        },
        methods: {
            async loadAddresses() {
                this.loading = true;
                this.error = "";
                try {
                    const payload = await this.request("/api/v1/addresses");
                    this.items = payload.data || [];
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "地址加载失败";
                } finally {
                    this.loading = false;
                }
            },
            validateForm() {
                const required = [
                    [this.form.receiverName, "请填写收货人"],
                    [this.form.receiverPhone, "请填写手机号"],
                    [this.form.province, "请填写省份"],
                    [this.form.city, "请填写城市"],
                    [this.form.district, "请填写区/县"],
                    [this.form.detailAddress, "请填写详细地址"]
                ];
                const missing = required.find(([value]) => !String(value || "").trim());
                if (missing) return missing[1];
                if (!/^[0-9+\-()\s]{6,20}$/.test(this.form.receiverPhone)) return "手机号格式不正确";
                return "";
            },
            async saveAddress() {
                const validationMessage = this.validateForm();
                if (validationMessage) {
                    window.appToast?.(validationMessage, "warning");
                    return;
                }

                this.saving = true;
                this.error = "";
                const body = {
                    receiverName: this.form.receiverName,
                    receiverPhone: this.form.receiverPhone,
                    province: this.form.province,
                    city: this.form.city,
                    district: this.form.district,
                    detailAddress: this.form.detailAddress,
                    isDefault: this.form.isDefault
                };
                try {
                    const editing = Boolean(this.form.addressId);
                    await this.request(editing ? `/api/v1/addresses/${this.form.addressId}` : "/api/v1/addresses", editing ? "PUT" : "POST", body);
                    this.resetForm();
                    await this.loadAddresses();
                    window.appToast?.(editing ? "地址修改成功" : "地址新增成功", "success");
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "地址保存失败";
                } finally {
                    this.saving = false;
                }
            },
            editAddress(item) {
                this.form = { ...item };
                document.getElementById("receiverName")?.focus();
                window.scrollTo({ top: 0, behavior: "smooth" });
            },
            async setDefault(item) {
                this.saving = true;
                this.error = "";
                try {
                    await this.request(`/api/v1/addresses/${item.addressId}/default`, "PUT");
                    await this.loadAddresses();
                    window.appToast?.("默认地址设置成功", "success");
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "默认地址设置失败";
                } finally {
                    this.saving = false;
                }
            },
            async deleteAddress(item) {
                if (!window.confirm(`确认删除 ${item.receiverName} 的地址吗？`)) return;
                this.saving = true;
                this.error = "";
                try {
                    await this.request(`/api/v1/addresses/${item.addressId}`, "DELETE");
                    if (this.form.addressId === item.addressId) this.resetForm();
                    await this.loadAddresses();
                    window.appToast?.("地址已删除", "success");
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "地址删除失败";
                } finally {
                    this.saving = false;
                }
            },
            resetForm() {
                this.form = emptyForm();
            },
            fullAddress(item) {
                return [item.province, item.city, item.district, item.detailAddress].filter(Boolean).join(" ");
            },
            async request(url, method = "GET", body = null) {
                const response = await fetch(url, {
                    method,
                    headers: { Accept: "application/json", ...(body ? { "Content-Type": "application/json" } : {}) },
                    body: body ? JSON.stringify(body) : null
                });
                const payload = await response.json().catch(() => null);
                if (!response.ok || !payload?.success) throw new Error(payload?.message || `请求失败（${response.status}）`);
                return payload;
            }
        }
    }).mount("#addressesApp");
})();
