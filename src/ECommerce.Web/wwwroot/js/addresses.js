(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                error: "",
                message: "",
                items: [],
                form: this.emptyForm()
            };
        },
        mounted() {
            this.loadAddresses();
        },
        methods: {
            emptyForm() {
                return {
                    addressId: null,
                    receiverName: "",
                    receiverPhone: "",
                    province: "",
                    city: "",
                    district: "",
                    detailAddress: "",
                    isDefault: false
                };
            },
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
            async saveAddress() {
                this.error = "";
                this.message = "";
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
                    if (this.form.addressId) {
                        await this.request(`/api/v1/addresses/${this.form.addressId}`, "PUT", body);
                        this.message = "地址修改成功";
                    } else {
                        await this.request("/api/v1/addresses", "POST", body);
                        this.message = "地址新增成功";
                    }

                    this.resetForm();
                    await this.loadAddresses();
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "地址保存失败";
                }
            },
            editAddress(item) {
                this.form = { ...item };
            },
            async setDefault(item) {
                this.error = "";
                this.message = "";
                try {
                    await this.request(`/api/v1/addresses/${item.addressId}/default`, "PUT");
                    this.message = "默认地址设置成功";
                    await this.loadAddresses();
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "默认地址设置失败";
                }
            },
            async deleteAddress(item) {
                if (!confirm(`确认删除 ${item.receiverName} 的地址吗？`)) {
                    return;
                }

                this.error = "";
                this.message = "";
                try {
                    await this.request(`/api/v1/addresses/${item.addressId}`, "DELETE");
                    this.message = "地址删除成功";
                    await this.loadAddresses();
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "地址删除失败";
                }
            },
            resetForm() {
                this.form = this.emptyForm();
            },
            async request(url, method = "GET", body = null) {
                const response = await fetch(url, {
                    method,
                    headers: {
                        "Accept": "application/json",
                        "Content-Type": "application/json"
                    },
                    body: body ? JSON.stringify(body) : null
                });
                const payload = await response.json();
                if (!response.ok || !payload.success) {
                    throw new Error(payload.message || `HTTP ${response.status}`);
                }

                return payload;
            }
        }
    }).mount("#addressesApp");
})();
