import { getByIdGql, updateGql, createGql } from "./queries";
import { runQuery, type QueryVariables, UserInput } from "./userQueries";
import { getUserInfo } from "./userInfo";
import { showLoading, hideLoading } from "./loading";

const formId = "registrationForm";
const internalFields = [
  "id",
  "_redirect_url",
  "origin",
  "createdAt",
  "updatedAt",
];

function buildItem(): UserInput {
  let user: UserInput = new UserInput();
  const form = document.querySelector(`form[id="${formId}"]`);
  var values = Object.values(form).reduce((obj, field) => {
    if (field.type === "checkbox") {
      obj[field.name] = field.checked;
    } else {
      obj[field.name] = field.value;
    }
    return obj;
  }, {});

  Object.keys(user).forEach((key) => {
    if (values[key]) {
      let n = typeof user[key];
      console.log(n);
      if (key === "pax" || key === "children") {
        user[key] = parseInt(values[key]);
      } else user[key] = values[key];
    }
  });

  return user;
}

async function createOrUpdate(): Promise<void> {
  showLoading();
  try {
    const user = buildItem();

    const data: QueryVariables = {
      id: user.id,
      _partitionKeyValue: user.id,
      item: user,
    };

    const existingUser = await runQuery(getByIdGql, data);

    var response: UserInput;
    if (existingUser) {
      data.item.updatedAt = new Date().toISOString();
      response = await runQuery(updateGql, data);
    } else {
      data.item.createdAt = new Date().toISOString();
      data.item.updatedAt = data.item.createdAt;
      response = await runQuery(createGql, data);
    }
    console.table(response);
    const redirectInput = document.querySelector<HTMLInputElement>(
      `form[id="${formId}"] input[name="_redirect_url"]`,
    );
    if (redirectInput) {
      const redirectUrl = redirectInput.value;
      if (redirectUrl) {
        try {
          const url = new URL(redirectUrl, window.location.href);
          window.location.href = url.href;
        } catch (e) {
          console.error("Invalid URL:", redirectUrl);
        }
      }
    }
  } finally {
    hideLoading();
  }
}

(async () => {
  const registrationForm = document.querySelector<HTMLFormElement>(
    `form[id="${formId}"]`,
  );
  if (registrationForm) {
    showLoading();
    try {
      const info = await getUserInfo();
      const email = registrationForm.querySelector<HTMLInputElement>(
        'input[name="email"]',
      );
      if (email) {
        email.value = info.userDetails;
      }
      const origin = registrationForm.querySelector<HTMLInputElement>(
        'input[name="origin"]',
      );
      if (origin) {
        origin.value = info.userDetails;
      } else {
        const hiddenInput: HTMLInputElement = document.createElement("input");
        hiddenInput.type = "hidden";
        hiddenInput.name = "origin";
        hiddenInput.value = info.userDetails;
        registrationForm.appendChild(hiddenInput);
      }

      const id =
        registrationForm.querySelector<HTMLInputElement>('input[name="id"]');
      if (id) {
        id.value = info.userId;
      }

      const user = await runQuery(getByIdGql, { id: info.userId });
      console.log(user);
      if (user && user.id) {
        Object.keys(user).forEach((key) => {
          const inputElement = registrationForm.querySelector<HTMLInputElement>(
            `input[name="${key}"]`,
          );
          if (inputElement) {
            if (inputElement.type === "checkbox") {
              inputElement.checked = user[key];
            } else {
              inputElement.value = user[key];
            }
          } else {
            const textArea =
              registrationForm.querySelector<HTMLTextAreaElement>(
                `textarea[name="${key}"]`,
              );
            if (textArea) {
              textArea.value = user[key];
            } else {
              const hiddenInput: HTMLInputElement =
                document.createElement("input");
              hiddenInput.type = "hidden";
              hiddenInput.name = key;
              hiddenInput.value = user[key];
              registrationForm.appendChild(hiddenInput);
            }
          }
        });
      }
    } finally {
      hideLoading();
    }
    registrationForm.addEventListener("submit", (e) => {
      e.preventDefault();
      createOrUpdate();
    });
  } else {
    var dataDiv = document.querySelector<HTMLDivElement>("#registrationData");
    if (dataDiv) {
      const table: HTMLTableElement = document.createElement("table");
      const info = await getUserInfo();
      const user = await runQuery(getByIdGql, { id: info.userId });
      Object.keys(user).forEach((key) => {
        if (internalFields.includes(key)) {
          return;
        }
        const keytd: HTMLTableCellElement = document.createElement("td");
        keytd.innerText = key + ":";
        keytd.style.textAlign = "right";
        const valuetd: HTMLTableCellElement = document.createElement("td");
        valuetd.innerText = user[key];
        const infoRow: HTMLTableRowElement = document.createElement("tr");
        infoRow.appendChild(keytd);
        infoRow.appendChild(valuetd);
        table.appendChild(infoRow);
      });
      dataDiv.replaceChildren(table);
    }
  }
})();
