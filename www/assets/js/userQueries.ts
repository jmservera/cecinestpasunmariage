
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

interface GraphError {
  locations: Array<object>;
  message: string;
  path: Array<string>;
}

export async function runQuery(
  query: string,
  data?: QueryVariables
): Promise<UserInput> {
  const response: { data: { user: UserInput }; errors: Array<GraphError> } =
    await execQuery(query, data);
  if (response.errors) {
    console.error(response.errors);
    throw new Error(response.errors[0].message);
  }
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
