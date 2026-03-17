
echo "Running functional flow"

curl http://localhost:5000/api/products

curl -X POST http://localhost:5000/api/cart -H "Content-Type: application/json" -d '{"productId":1,"quantity":2}'

curl -X POST http://localhost:5000/api/orders

curl http://localhost:5000/api/orders
