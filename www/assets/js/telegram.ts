import { hideLoading, showLoading } from "./loading";
import { getTranslation } from "./i18n";


(async () => {
    showLoading();
    try {
        //extract the data from the query parameter id
        const urlParams = new URLSearchParams(window.location.search);
        const encodedChatUser = urlParams.get('id');
        // base64 decode chatUser and convert to json
        const chatUser = JSON.parse(atob(encodedChatUser));

        if (chatUser.UserId && chatUser.ChatId) {

            const response = await fetch(`/api/AuthenticateBot`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(chatUser)
            });
            response.status === 200 ? window.location.href = `https://telegram.me/${chatUser.BotName}` : alert("Error");
            document.body.insertAdjacentText('beforeend', getTranslation('telegram.registered'));
        }
        else {
            alert(`Error ${atob(encodedChatUser)}`);
        }
        hideLoading();
    } catch (error) {
        console.error(error);
        hideLoading();
    }
})();