# Takes input license key and creates a .env file NEW_RELIC_LICENSE_KEY=license_key as well as a newrelic-infra.yml file with license_key: license_key

# Get license key from user
echo "Enter your New Relic license key:"
read license_key

# Check if .env file exists
if [ -f .env ]; then
    echo ".env file exists"
else
    echo ".env file does not exist"
    echo "Creating .env file"
    touch .env

    # Write OTLP endpoint and headers to .env file
    echo "Writing OTLP endpoint and headers to .env file"
    echo "OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.nr-data.net:4317" >> .env
    echo "OTEL_EXPORTER_OTLP_HEADERS=api-key=$license_key" >> .env

    # Write license key to .env file
    echo "Writing license key to .env file"
    echo "NEW_RELIC_LICENSE_KEY=$license_key" >> .env
fi

# Check if newrelic-infra.yml file exists
if [ -f newrelic-infra.yml ]; then
    echo "newrelic-infra.yml file exists"
else
    echo "newrelic-infra.yml file does not exist"
    echo "Creating newrelic-infra.yml file"
    touch newrelic-infra.yml
    
    # Write license key to newrelic-infra.yml file
    echo "Writing license key to newrelic-infra.yml file"
    echo "license_key: $license_key" > newrelic-infra.yml
fi


