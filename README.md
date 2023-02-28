# Queued Processing

Implementation of [Async Request Reply](https://learn.microsoft.com/en-us/azure/architecture/patterns/async-request-reply) through kafka topic processing.

## Running locally

- Run the [configure](./configure.sh) file in this directory and provide a valid New Relic license key.
- From a terminal instance in this directory, run

```sh
docker compose up --build
```

- Open a browser instance to http://localhost:8080/swagger
  **OR**
- Run the locust tests in ./tests via `locust -f .\test.py -H http://localhost:8080 -u 10`
