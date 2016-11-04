# c.sharp.akka.net.etl
A sample ETL project that reads data from an AMQP server and transforms it using Akka actors and loads in to an Http endpoint.

It demonstrates the use of the following Techniques:

- Writing a program that can run both as a console application and a windows service
- Writing a consumer for a RabbitMQ AMQP server
- Basic usage of Akka Actors to divide work across multiple cores and processes
- Marshalling XML and UnMarshalling JSON objects.
- Calling authenticated and Https restful services and web endpoints
- generating an installer that allows for configuration values to be specified at install time

