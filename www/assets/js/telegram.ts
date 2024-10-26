import { hideLoading, showLoading } from "./loading";
import { getTranslation } from "./i18n";
import { postEvent } from "@telegram-apps/bridge";

(async () => {
  showLoading();
  try {
    var msgDiv = document.getElementById(
      "telegramRegistration",
    ) as HTMLDivElement;

    msgDiv.innerText = getTranslation("telegram.registering");
    //extract the data from the query parameter id
    const urlParams = new URLSearchParams(window.location.search);
    const encodedChatUser = urlParams.get("id");
    // base64 decode chatUser and convert to json
    const chatUser = JSON.parse(atob(encodedChatUser));

    if (chatUser.UserId && chatUser.ChatId) {
      const response = await fetch(`/api/AuthenticateBot`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(chatUser),
      });
      if (response.status === 200) {
        msgDiv.innerText = getTranslation("telegram.registered");
        // after 2 seconds redirect to the telegram bot
        setTimeout(() => {
          postEvent("web_app_close");
        }, 2000);
      } else {
        msgDiv.innerText = getTranslation("telegram.error");
      }
    } else {
      msgDiv.innerText = getTranslation("telegram.error");
      console.error(`Error ${atob(encodedChatUser)}`);
    }
  } catch (error) {
    console.error(error);
  } finally {
    hideLoading();
  }
})();
