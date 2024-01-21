/// <reference lib="es2015" />
/// <reference lib="dom" />

const usr: string =
  "{\nid\norigin\nemail\nname\nsurname\npartnerName\npax\nchildren\nalergies\ncomments\ncreatedAt\nupdatedAt\n}";

export const queries = {
  getByIdGql: "query getById($id: ID!) {\nuser_by_pk(id: $id) " + usr + "\n}",
  createGql:
    "mutation create($item: CreateUserInput!) {\ncreateUser(item: $item) " +
    usr +
    "\n}",
  updateGql:
    "mutation update($id: ID!, $_partitionKeyValue: String!, $item: UpdateUserInput!) " +
    "{\nupdateUser(id: $id, _partitionKeyValue: $_partitionKeyValue, item: $item) " +
    usr +
    "\n}",
  getAllUsersGql: "{\nusers {\nitems " + usr + "\n}\n}",
};

export class UserInput {
  id: string;
  origin: string;
  email: string;
  name: string;
  surname: string;
  children?: number;
  pax: number;
  partnerName?: string;  
  alergies?: string;
  comments?: string;
  createdAt?: string;
  updatedAt?: string;
}

export type QueryVariables = {
  id?: string;
  _partitionKeyValue?: string;
  item?: UserInput;
};

async function execQuery(query: string, data?: QueryVariables): Promise<any> {
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

  return await result.json();
}

export async function runQuery(
  query: string,
  data?: QueryVariables
): Promise<UserInput> {
  const response: { data: { user: UserInput } } = await execQuery(query, data);
  return response.data[Object.keys(response.data)[0]];
}

export async function runQueryAll(
  query: string,
  data?: QueryVariables
): Promise<UserInput[]> {
  const response: { data: { users: { items: UserInput[] } } } = await execQuery(
    query,
    data
  );
  return response.data[Object.keys(response.data)[0]].items;
}
