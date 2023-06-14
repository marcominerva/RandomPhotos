function photos(language) {
    Alpine.data("photos", () => ({
        language: language,
        isBusy: false,
        errorMessage: null,

        imageUrl: null,
        description: null,

        generatePhoto: async function () {
            if (this.isBusy)
                return;

            this.errorMessage = null;
            this.imageUrl = null;
            this.description = null;
            this.isBusy = true;

            try {
                const response = await generateRandomPhoto(language);
                const content = await response.json();

                this.isBusy = false;

                this.errorMessage = GetErrorMessage(response.status, content);

                if (this.errorMessage == null) {
                    // The request has succeeded.
                    this.description = content.description;
                    this.imageUrl = content.url;
                }
            } catch (error) {
                this.isBusy = false;
                this.errorMessage = error;
            }
        }
    }));
}