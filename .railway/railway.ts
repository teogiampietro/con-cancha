import { defineRailway, github, postgres, preserve, project, service } from "railway/iac";

const REPOSITORY = "teogiampietro/con-cancha";
const BRANCH = "main";
const ROOT_DIRECTORY = ".";
const PORT = "8080";

export default defineRailway(() => {
  const db = postgres("concancha-db");

  const api = service("concancha-api", {
    source: github(REPOSITORY, { branch: BRANCH, rootDirectory: ROOT_DIRECTORY }),
    healthcheck: "/health",
    env: {
      PROJECT: "ConCancha.Api",
      PORT,
      ConnectionStrings__Default: db.env.DATABASE_URL,
      Database__MigrateOnStartup: "true",
      Jwt__Key: preserve(),
      Web__BaseUrl: preserve(),
      Email__From: preserve(),
      Email__ResendApiKey: preserve(),
    },
  });

  const web = service("concancha-web", {
    source: github(REPOSITORY, { branch: BRANCH, rootDirectory: ROOT_DIRECTORY }),
    env: {
      PROJECT: "ConCancha.Web",
      PORT,
      Api__BaseUrl: preserve(),
    },
  });

  return project("concancha", {
    resources: [db, api, web],
  });
});
