/// <reference lib="es2015" />
/// <reference lib="dom" />

import { runQuery, QueryVariables, queries, UserInput } from "./queries";
import { getUserInfo } from "./userInfo";

function buildItem(): UserInput {
  let user: UserInput = new UserInput();
  const form = document.querySelector('form[id="registrationForm"]');
  var values = Object.values(form).reduce((obj, field) => {
    obj[field.name] = field.value;
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
  const user = buildItem();

  const data: QueryVariables = {
    id: user.id,
    _partitionKeyValue: user.id,
    item: user,
  };

  const existingUser = await runQuery(queries.getByIdGql, data);

  var response: UserInput;
  if (existingUser) {
    data.item.updatedAt = new Date().toISOString();
    response = await runQuery(queries.updateGql, data);
  } else {
    data.item.createdAt = new Date().toISOString();
    data.item.updatedAt = data.item.createdAt;
    response = await runQuery(queries.createGql, data);
  }
  console.table(response);
}

(async () => {
  const info = await getUserInfo();
  const registrationForm = document.querySelector<HTMLFormElement>(
    'form[id="registrationForm"]'
  );
  if (registrationForm) {
    const email = registrationForm.querySelector<HTMLInputElement>(
      'input[name="email"]'
    );
    if (email) {
      email.value = info.userDetails;
    }
    const origin = registrationForm.querySelector<HTMLInputElement>(
      'input[name="origin"]'
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

    const user = await runQuery(queries.getByIdGql, { id: info.userId });
    console.log(user);
    if (user && user.id) {
      Object.keys(user).forEach((key) => {
        const inputElement = registrationForm.querySelector<HTMLInputElement>(
          `input[name="${key}"]`
        );
        if (inputElement) {
          inputElement.value = user[key];
        } else {
          const textArea = registrationForm.querySelector<HTMLTextAreaElement>(
            `textarea[name="${key}"]`
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

    registrationForm.addEventListener("submit", (e) => {
      e.preventDefault();
      createOrUpdate();
    });
  }
})();
