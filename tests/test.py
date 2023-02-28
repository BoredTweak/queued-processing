import random
from locust import HttpUser, task, constant

class ApiUser(HttpUser):
    wait_time = constant(0.5)

    @task
    def sends_number(self):
        '''sends a number to the API and checks the response is a guid'''
        random_number = random.randint(1, 10000)
        response = self.client.post("/FizzBuzz?input=" + str(random_number))
        assert response.text != "", "Response text is empty"
        assert response.status_code == 200, "Response status code is not 200"
        