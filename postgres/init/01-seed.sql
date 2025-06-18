CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255),
    price DECIMAL
);
INSERT INTO products (name, price) VALUES ('Product A', 100.00) ON CONFLICT DO NOTHING;
INSERT INTO products (name, price) VALUES ('Product B', 75.00) ON CONFLICT DO NOTHING;
INSERT INTO products (name, price) VALUES ('Product C', 55.00) ON CONFLICT DO NOTHING;