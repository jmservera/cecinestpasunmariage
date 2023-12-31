type ClientPrincipal = {
  userDetails: string;
  userId: string;
};

type UserInput = {
  id: string;
  email: string;
  name: string;
  pax: number;
};

type QueryVariables = {
  id?: string;
  _partitionKeyValue?: string;
  item?: UserInput;
};

async function getUserInfo(): Promise<ClientPrincipal> {
  const response = await fetch("/.auth/me");
  const payload = await response.json();
  const { clientPrincipal } = payload;
  return clientPrincipal;
}

const getByIdGql =
  "query getById($id: ID!) {\nuser_by_pk(id: $id) {\nid\nemail\nname\npax\n}\n}";

const createGql =
  "mutation create($item: CreateUserInput!) {\ncreateUser(item: $item) {\nid\nemail\nname\npax\n}\n}";

const updateGql =
  "mutation update($id: ID!, $_partitionKeyValue: String!, $item: UpdateUserInput!) " +
  "{\nupdateUser(id: $id, _partitionKeyValue: $_partitionKeyValue, item: $item) {\nid\nemail\nname\npax\n}\n}";

async function runQuery(
  query: string,
  data: QueryVariables
): Promise<UserInput> {
  const createQuery = {
    query: query,
    variables: data,
  };

  const endpoint = "/data-api/graphql";
  const result = await fetch(endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(createQuery),
  });

  const response: { data: { user: UserInput } } = await result.json();
  return response.data[Object.keys(response.data)[0]];
}

function buildItem(): UserInput {
  const id = document.querySelector<HTMLInputElement>('input[name="id"]');
  const email = document.querySelector<HTMLInputElement>('input[name="email"]');
  const name = document.querySelector<HTMLInputElement>('input[name="name"]');
  const pax = document.querySelector<HTMLInputElement>('input[name="pax"]');

  return {
    id: id ? id.value : "",
    email: email ? email.value : "",
    name: name ? name.value : "",
    pax: pax ? parseInt(pax.value) : 1,
  };
}

async function createOrUpdate(): Promise<void> {
  const user = buildItem();

  const data: QueryVariables = {
    id: user.id,
    _partitionKeyValue: user.id,
    item: user,
  };

  const existingUser = await runQuery(getByIdGql, data);

  var response: UserInput;
  if (existingUser) {
    response = await runQuery(updateGql, data);
  } else {
    response = await runQuery(createGql, data);
  }
  console.table(response);
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

  const user = await runQuery(getByIdGql, { id: info.userId });
  console.log(user);
  if (user && user.id) {
    const name = document.querySelector<HTMLInputElement>('input[name="name"]');
    if (name) {
      name.value = user.name;
    }
    const pax = document.querySelector<HTMLInputElement>('input[name="pax"]');
    if (pax) {
      pax.value = user.pax.toString();
    }
    const email = document.querySelector<HTMLInputElement>(
      'input[name="email"]'
    );
    if (email) {
      email.value = user.email;
    }
  }

  const form = document.querySelector<HTMLFormElement>("form");
  if (form) {
    form.addEventListener("submit", (e) => {
      e.preventDefault();
      createOrUpdate();
    });
  }
})();
