/// <reference lib="es2015" />
/// <reference lib="dom" />

import { runQuery, QueryVariables, queries, UserInput } from "./queries";
import { getUserInfo } from "./userInfo";

function buildItem(): UserInput {
  let user: UserInput = new UserInput();
  Object.keys(user).forEach((key) => {
    const el = document.querySelector<HTMLInputElement>(`input[name="${key}"]`);
    if (el) {
      let n = typeof user[key];
      console.log(n);
      if (key === "pax") {
        user[key] = parseInt(el.value);
      } else user[key] = el.value;
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
  const email = document.querySelector<HTMLInputElement>('input[name="email"]');
  if (email) {
    email.value = info.userDetails;
  }
  const origin = document.querySelector<HTMLInputElement>(
    'input[name="origin"]'
  );
  if (origin) {
    origin.value = info.userDetails;
  } else {
    const form = document.querySelector<HTMLFormElement>("form");
    const hiddenInput: HTMLInputElement = document.createElement("input");
    hiddenInput.type = "hidden";
    hiddenInput.name = "origin";
    hiddenInput.value = info.userDetails;
    form.appendChild(hiddenInput);
  }

  const id = document.querySelector<HTMLInputElement>('input[name="id"]');
  if (id) {
    id.value = info.userId;
  }

  const user = await runQuery(queries.getByIdGql, { id: info.userId });
  console.log(user);
  if (user && user.id) {
    Object.keys(user).forEach((key) => {
      const el = document.querySelector<HTMLInputElement>(
        `input[name="${key}"]`
      );
      if (el) {
        el.value = user[key];
      } else {
        const form = document.querySelector<HTMLFormElement>("form");
        const hiddenInput: HTMLInputElement = document.createElement("input");
        hiddenInput.type = "hidden";
        hiddenInput.name = key;
        hiddenInput.value = user[key];
        form.appendChild(hiddenInput);
      }
    });
  }

  const form = document.querySelector<HTMLFormElement>("form");
  if (form) {
    form.addEventListener("submit", (e) => {
      e.preventDefault();
      createOrUpdate();
    });
  }
})();
