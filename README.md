docker build -t payments-service:v1 .
docker run -d --name payments-db --net mynet1 mongo
docker run -d --name payments-service --net mynet1 -p 8000:8080 -e DB_HOST="mongodb://payments-db" -e USERS_SERVICE=users-service:8080 payments-service:v1
