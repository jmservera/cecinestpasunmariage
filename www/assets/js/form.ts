type ClientPrincipal = {
  userDetails: string;
  userId: string;
};

type CreateUserInput = {
  id: string;
  email: string;
  name: string;
  pax: number;
};

async function getUserInfo(): Promise<ClientPrincipal> {
  const response = await fetch("/.auth/me");
  const payload = await response.json();
  const { clientPrincipal } = payload;
  return clientPrincipal;
}

(async () => {
  const info = await getUserInfo();
  console.log(info);
  const email = document.querySelector<HTMLInputElement>('input[name="email"]');
  if (email) {
    email.value = info.userDetails;
  }
  const id = document.querySelector<HTMLInputElement>('input[name="id"]');
  if (id) {
    id.value = info.userId;
  }

  const form = document.querySelector<HTMLFormElement>("form");
  if (form) {
    form.addEventListener("submit", (e) => {
      e.preventDefault();
      create();
    });
  }
})();

async function create(): Promise<void> {
  const id = document.querySelector<HTMLInputElement>('input[name="id"]');
  const email = document.querySelector<HTMLInputElement>('input[name="email"]');
  const name = document.querySelector<HTMLInputElement>('input[name="name"]');
  const pax = document.querySelector<HTMLInputElement>('input[name="pax"]');

  const data: CreateUserInput = {
    id: id ? id.value : "",
    email: email ? email.value : "",
    name: name ? name.value : "",
    pax: pax ? parseInt(pax.value) : 1,
  };

  const gql =
    "mutation create($item: CreateUserInput!) {\ncreateUser(item: $item) {\nid\nemail\nname\npax\n}\n}";

  const query = {
    query: gql,
    variables: {
      item: data,
    },
  };

  const endpoint = "/data-api/graphql";
  const result = await fetch(endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(query),
  });

  const response: { data: { createUser: CreateUserInput } } =
    await result.json();
  console.table(response.data.createUser);
}
