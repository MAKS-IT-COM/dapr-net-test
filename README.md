# Dapr with ASP.NET Core in Docker Compose and Kubernetes deployment test

The aim of this project is to provide a simple reference on how to perform local development and deploy a C# Dapr-powered microservices application in Kubernetes. This project demonstrates the deployment of a sample application using RabbitMQ for messaging and Redis for state management, leveraging Dapr to simplify the development of distributed applications.

## Project Structure

The project includes the following components:

* RabbitMQ: A message broker used for pub/sub messaging.
* Redis: A key-value store used for state management.
* Publisher App: A C# microservice that publishes messages to RabbitMQ and set Redis shared state.
* Subscriber App: A C# microservice that subscribes to messages from RabbitMQ and read Redis shared state.

## Requirements

To deploy this C# microservices application in Kubernetes with Dapr support, you need the following:

* Kubernetes: A running Kubernetes cluster (version 1.18 or later recommended).
kubectl command-line tool configured to interact with your Kubernetes cluster.
* Configured Storage Class: A configured storage class in your Kubernetes cluster to provision persistent storage for stateful components like Redis.
* Dapr: Dapr CLI installed (version 1.0 or later). Dapr initialized in your Kubernetes cluster.

In case you decide to build images from source code on your own, you also need:

* Container registry: A configured container registry, such as Harbor or Docker Hub.

In case of local development (no Kubernetes required):

* Visual Studio 2022 Community: An integrated development environment (IDE) for developing C# applications.
* .NET 8 SDK: The software development kit for building and running .NET applications.
* Rancher Desktop or Docker Desktop: Tools for running Docker containers on your local machine.

## Deployment

The deployment is done using Kubernetes manifests for each component, including Dapr components for pub/sub and state management.
Sample application can be installed via [Yaml](#yaml-deployment) or [Powershell](#powershell-deployment) deployment

### Yaml deployment

```yaml
# RabbitMQ

apiVersion: apps/v1
kind: Deployment
metadata:
  name: rabbitmq
  namespace: dapr-test
spec:
  replicas: 1
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
    spec:
      containers:
      - name: rabbitmq
        image: rabbitmq:3-management
        ports:
        - containerPort: 5672 # RabbitMQ port
        - containerPort: 15672 # RabbitMQ management port
        env:
        - name: RABBITMQ_DEFAULT_USER
          value: admin
        - name: RABBITMQ_DEFAULT_PASS
          value: password
        - name: RABBITMQ_DEFAULT_VHOST
          value: "/"

---

apiVersion: v1
kind: Service
metadata:
  name: rabbitmq-service
  namespace: dapr-test
spec:
  selector:
    app: rabbitmq
  ports:
  - name: amqp
    protocol: TCP
    port: 5672
    targetPort: 5672
  - name: management
    protocol: TCP
    port: 15672
    targetPort: 15672

---

# Redis

apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
  namespace: dapr-test
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:6.2.6
        ports:
        - containerPort: 6379
  
---

apiVersion: v1
kind: Service
metadata:
  name: redis-service
  namespace: dapr-test
spec:
  selector:
    app: redis
  ports:
  - protocol: TCP
    port: 6379
    targetPort: 6379

---

# Publisher App

apiVersion: apps/v1
kind: Deployment
metadata:
  name: publisher
  namespace: dapr-test
spec:
  replicas: 1
  selector:
    matchLabels:
      app: publisher
  template:
    metadata:
      labels:
        app: publisher
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "dapr-test-publisher"
        dapr.io/app-port: "5000"  # Adjust to your service port
    spec:
      containers:
        - name: publisher
          image: cr.maks-it.com/dapr-test/publisher:latest
          imagePullPolicy: Always
          ports:
            - containerPort: 5000  # Match your internal app port
          env:
            - name: ASPNETCORE_HTTP_PORTS
              value: "5000"
            - name: ASPNETCORE_ENVIRONMENT
              value: Development

---

apiVersion: v1
kind: Service
metadata:
  name: publisher-service
  namespace: dapr-test
spec:
  selector:
    app: publisher
  ports:
    - protocol: TCP
      port: 80
      targetPort: 5000  # Match the internal port of the publisher service

---

# Subscriber App

apiVersion: apps/v1
kind: Deployment
metadata:
  name: subscriber
  namespace: dapr-test
spec:
  replicas: 1
  selector:
    matchLabels:
      app: subscriber
  template:
    metadata:
      labels:
        app: subscriber
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "dapr-test-subscriber"
        dapr.io/app-port: "5000"  # Adjust to match the subscriber service port
    spec:
      containers:
        - name: subscriber
          image: cr.maks-it.com/dapr-test/subscriber:latest
          imagePullPolicy: Always
          ports:
            - containerPort: 5000  # Match the internal port of the subscriber service
          env:
            - name: ASPNETCORE_HTTP_PORTS
              value: "5000"
            - name: ASPNETCORE_ENVIRONMENT
              value: Development

---

# Dapr PubSub

apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: dapr-test-pubsub
  namespace: dapr-test
spec:
  type: pubsub.rabbitmq
  version: v1
  metadata:
  - name: connectionString
    value: "amqp://admin:password@rabbitmq:5672"
  - name: durable
    value: "false"
  - name: deletedWhenUnused
    value: "false"
  - name: autoAck
    value: "true"
  - name: reconnectWait
    value: "0"
  - name: concurrency
    value: parallel

---

# Dapr StateStore

apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: dapr-test-statestore
  namespace: dapr-test
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: redis:6379
  - name: keyPrefix
    value: none

---

apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: dapr-test-privatestatestore
  namespace: dapr-test
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: redis:6379

---

apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: dapr-test-actorsstatestore
  namespace: dapr-test
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: redis:6379
  - name: actorStateStore 
    value: "true"

```

### Powershell deployment

```powershell
kubectl create namespace dapr-test `

# RabbitMQ
@{
    apiVersion = "apps/v1"
    kind = "Deployment"
    metadata = @{
        name = "rabbitmq"
        namespace = "dapr-test"
    }
    spec = @{
        replicas = 1
        selector = @{
            matchLabels = @{
                app = "rabbitmq"
            }
        }
        template = @{
            metadata = @{
                labels = @{
                    app = "rabbitmq"
                }
            }
            spec = @{
                containers = @(
                    @{
                        name = "rabbitmq"
                        image = "rabbitmq:3-management"
                        ports = @(
                            @{
                                containerPort = 5672  # RabbitMQ port
                            },
                            @{
                                containerPort = 15672 # RabbitMQ management port
                            }
                        )
                        env = @(
                            @{
                                name = "RABBITMQ_DEFAULT_USER"
                                value = "admin"
                            },
                            @{
                                name = "RABBITMQ_DEFAULT_PASS"
                                value = "password"
                            },
                            @{
                                name = "RABBITMQ_DEFAULT_VHOST"
                                value = "/"
                            }
                        )
                    }
                )
            }
        }
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

@{
    apiVersion = "v1"
    kind = "Service"
    metadata = @{
        name = "rabbitmq-service"
        namespace = "dapr-test"
    }
    spec = @{
        selector = @{
            app = "rabbitmq"
        }
        ports = @(
            @{
                name = "amqp"
                protocol = "TCP"
                port = 5672
                targetPort = 5672
            },
            @{
                name = "management"
                protocol = "TCP"
                port = 15672
                targetPort = 15672
            }
        )
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

# Redis
@{
    apiVersion = "apps/v1"
    kind = "Deployment"
    metadata = @{
        name = "redis"
        namespace = "dapr-test"
    }
    spec = @{
        replicas = 1
        selector = @{
            matchLabels = @{
                app = "redis"
            }
        }
        template = @{
            metadata = @{
                labels = @{
                    app = "redis"
                }
            }
            spec = @{
                containers = @(
                    @{
                        name = "redis"
                        image = "redis:6.2.6"
                        ports = @(
                            @{
                                containerPort = 6379  # Redis port
                            }
                        )
                    }
                )
            }
        }
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

@{
    apiVersion = "v1"
    kind = "Service"
    metadata = @{
        name = "redis-service"
        namespace = "dapr-test"
    }
    spec = @{
        selector = @{
            app = "redis"
        }
        ports = @(
            @{
                protocol = "TCP"
                port = 6379
                targetPort = 6379
            }
        )
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

# Publisher App
@{
    apiVersion = "apps/v1"
    kind = "Deployment"
    metadata = @{
        name = "publisher"
        namespace = "dapr-test"
    }
    spec = @{
        replicas = 1
        selector = @{
            matchLabels = @{
                app = "publisher"
            }
        }
        template = @{
            metadata = @{
                labels = @{
                    app = "publisher"
                }
                annotations = @{
                    "dapr.io/enabled" = "true"
                    "dapr.io/app-id" = "dapr-test-publisher"
                    "dapr.io/app-port" = "5000"  # Adjust to your service port
                }
            }
            spec = @{
                containers = @(
                    @{
                        name = "publisher"
                        image = "cr.maks-it.com/dapr-test/publisher:latest"  # Corrected image name
                        imagePullPolicy = "Always"
                        ports = @(
                            @{
                                containerPort = 5000  # Match your internal app port
                            }
                        )
                        env = @(
                            @{
                                name = "ASPNETCORE_HTTP_PORTS"
                                value = "5000"
                            },
                            @{
                                name = "ASPNETCORE_ENVIRONMENT"
                                value = "Development"
                            }
                        )
                    }
                )
            }
        }
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

@{
    apiVersion = "v1"
    kind = "Service"
    metadata = @{
        name = "publisher-service"
        namespace = "dapr-test"
    }
    spec = @{
        selector = @{
            app = "publisher"
        }
        ports = @(
            @{
                protocol = "TCP"
                port = 80
                targetPort = 5000  # Match the internal port of the publisher service
            }
        )
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

# Subscriber App
@{
    apiVersion = "apps/v1"
    kind = "Deployment"
    metadata = @{
        name = "subscriber"
        namespace = "dapr-test"
    }
    spec = @{
        replicas = 1
        selector = @{
            matchLabels = @{
                app = "subscriber"
            }
        }
        template = @{
            metadata = @{
                labels = @{
                    app = "subscriber"
                }
                annotations = @{
                    "dapr.io/enabled" = "true"
                    "dapr.io/app-id" = "dapr-test-subscriber"
                    "dapr.io/app-port" = "5000"  # Adjust to match the subscriber service port
                }
            }
            spec = @{
                containers = @(
                    @{
                        name = "subscriber"
                        image = "cr.maks-it.com/dapr-test/subscriber:latest"
                        imagePullPolicy = "Always"
                        ports = @(
                            @{
                                containerPort = 5000  # Match the internal port of the subscriber service
                            }
                        )
                        env = @(
                            @{
                                name = "ASPNETCORE_HTTP_PORTS"
                                value = "5000"
                            },
                            @{
                                name = "ASPNETCORE_ENVIRONMENT"
                                value = "Development"
                            }
                        )
                    }
                )
            }
        }
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

# Dapr PubSub
@{
    apiVersion = "dapr.io/v1alpha1"
    kind = "Component"
    metadata = @{
        name = "dapr-test-pubsub"
        namespace = "dapr-test"
    }
    spec = @{
        type = "pubsub.rabbitmq"
        version = "v1"
        metadata = @(
            @{
                name = "connectionString"
                value = "amqp://admin:password@rabbitmq-service:5672"
            },
            @{
                name = "durable"
                value = "false"
            },
            @{
                name = "deletedWhenUnused"
                value = "false"
            },
            @{
                name = "autoAck"
                value = "true"
            },
            @{
                name = "reconnectWait"
                value = "0"
            },
            @{
                name = "concurrency"
                value = "parallel"
            }
        )
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

# Dapr StateStore
@{
    apiVersion = "dapr.io/v1alpha1"
    kind = "Component"
    metadata = @{
        name = "dapr-test-statestore"
        namespace = "dapr-test"
    }
    spec = @{
        type = "state.redis"
        version = "v1"
        metadata = @(
            @{
                name = "redisHost"
                value = "redis-service:6379"
            },
            @{
                name = "keyPrefix"
                value = "none"
            }
        )
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `

@{
    apiVersion = "dapr.io/v1alpha1"
    kind = "Component"
    metadata = @{
        name = "dapr-test-privatestatestore"
        namespace = "dapr-test"
    }
    spec = @{
        type = "state.redis"
        version = "v1"
        metadata = @(
            @{
                name = "redisHost"
                value = "redis-service:6379"
            }
        )
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f - `


@{
    apiVersion = "dapr.io/v1alpha1"
    kind = "Component"
    metadata = @{
        name = "dapr-test-actorsstatestore"
        namespace = "dapr-test"
    }
    spec = @{
        type = "state.redis"
        version = "v1"
        metadata = @(
            @{
                name = "redisHost"
                value = "redis-service:6379"
            },
            @{
                name = "actorStateStore"
                value = "true"
            }
        )
    }
} | ConvertTo-Json -Depth 10 | kubectl apply -f -
```

## Test

* You have to port forward `publisher-service` and go to `/swagger` path in your browser.
* Publish a sample mesessage
* If everything is ok, you will see logs with your published message in subscriber service

## Summary

This project provides a reference for deploying a C# microservices application in Kubernetes with Dapr support. It includes configurations for RabbitMQ and Redis, as well as the publisher and subscriber applications. The Dapr components for pub/sub and state management are also configured to demonstrate how to leverage Dapr for building distributed applications.

## Contribution

Contributions to this project are welcome! Please fork the repository and submit a pull request with your changes. If you encounter any issues or have feature requests, feel free to open an issue on GitHub.

## Contact

If you have any questions or need further assistance, feel free to reach out:

- **Email**: [maksym.sadovnychyy@gmail.com](mailto:maksym.sadovnychyy@gmail.com)

## License

This project is licensed under the MIT License. See the full license text below.

---

### MIT License

```
MIT License

Copyright (c) 2024 Maksym Sadovnychyy (MAKS-IT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
