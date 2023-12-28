async function getUserInfo() {
  const response = await fetch("/.auth/me");
  const payload = await response.json();
  const { clientPrincipal } = payload;
  return clientPrincipal;
}

(async () => {
  const info = await getUserInfo();
  console.log(info);
  const email = document.querySelector('input[name="email"]');
  email.value = info.userDetails;
  const id = document.querySelector('input[name="id"]');
  id.value = info.userId;

  const form = document.querySelector("form");
  form.addEventListener("submit", (e) => {
    e.preventDefault();
    create();
  });
})();

async function create() {
  const id = document.querySelector('input[name="id"]');
  const email = document.querySelector('input[name="email"]');

  const data = {
    id: id.value,
    email: email.value,
  };

  const gql = `mutation create($item: CreateUserInput!) {
    createUser(item: $item) {
      id
      email
    }
  }`;

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

  const response = await result.json();
  console.table(response.data.createUser);
}
