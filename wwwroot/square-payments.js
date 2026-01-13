let payments;
let card;

window.squarePayments = {
    initialize: async function (applicationId, locationId) {
        if (!window.Square) {
            throw new Error('Square.js failed to load properly');
        }
        
        // Clean up if already initialized
        if (card) {
            try {
                await card.destroy();
            } catch (e) {
                console.warn('Error destroying card', e);
            }
        }

        payments = window.Square.payments(applicationId, locationId);
        try {
            card = await payments.card();
            await card.attach('#card-container');
            return true;
        } catch (e) {
            console.error('Initializing Card failed', e);
            return false;
        }
    },
    tokenize: async function () {
        const result = await card.tokenize();
        if (result.status === 'OK') {
            return result.token;
        } else {
            let errorMessage = 'Tokenization failed';
            if (result.errors && result.errors.length > 0) {
                errorMessage = result.errors[0].message;
            }
            throw new Error(errorMessage);
        }
    }
};
