function photos(language) {
    Alpine.data("photos", () => ({
        language: language,
        isBusy: false,
        errorMessage: null,

        description: null,
        imageUrl: null,

        generatePhoto: async function () {
            this.errorMessage = null;

            while (this.isBusy)
            {
                await sleep(100);
            }

            this.description = null;
            this.imageUrl = null;
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