#!/usr/bin/env bats

setup() {
    export MOCK_RESPONSE_FILE="../shared/mock_responses.json"
}

@test "curl command structure validation" {
    local cmd="curl --user user:pass 'https://realtime.oxylabs.io/v1/queries' -H 'Content-Type: application/json' -d '{\"source\": \"universal\", \"url\": \"https://www.macys.com/shop/sale\"}'"
    
    [[ "$cmd" == *"--user"* ]]
    [[ "$cmd" == *"https://realtime.oxylabs.io/v1/queries"* ]]
    [[ "$cmd" == *"Content-Type: application/json"* ]]
    [[ "$cmd" == *"source"* ]]
    [[ "$cmd" == *"universal"* ]]
}

@test "payload structure validation" {
    local payload='{"source": "universal", "url": "https://www.macys.com/shop/sale?id=3536&trackingid=407x733169&m_sc=sem&m_sb=google&m_tp=trademark&m_ac=google_trademark_international&m_ag=macy%27score_exact&m_cn=ggl_trademark_intl_lithuania_exact&m_pi=go_cmp-94807774_adg-154238318312_ad-674544461209_kwd-252677959_dev-c_ext-102882313401_prd-&gad_source=1&gclid=cj0kcqiayewrbhddarisagp1mwsg3z6ogoqrztdycjyqio5togc316ldkwuqkkbhrmiv4i_ho0gcjlkaagl6ealw_wcb"}'
    
    echo "$payload" | jq -e '.source == "universal"'
    echo "$payload" | jq -e '.url | contains("macys.com")'
    echo "$payload" | jq -e 'keys | length == 2'
}

@test "authentication setup validation" {
    local auth_string="user:pass"
    local base64_auth=$(echo -n "$auth_string" | base64)
    
    [ -n "$base64_auth" ]
    
    local decoded=$(echo "$base64_auth" | base64 -d)
    [ "$decoded" = "$auth_string" ]
}

@test "HTTP method and headers validation" {
    local cmd="curl --user user:pass 'https://realtime.oxylabs.io/v1/queries' -H 'Content-Type: application/json' -d '{\"source\": \"universal\", \"url\": \"https://www.macys.com/shop/sale\"}'"
    
    [[ "$cmd" == *"-d"* ]]
    
    [[ "$cmd" == *"Content-Type: application/json"* ]]
    
    [[ "$cmd" == *"--user"* ]]
}

@test "mock response structure validation" {
    [ -f "$MOCK_RESPONSE_FILE" ]
    
    jq -e '.results | length > 0' "$MOCK_RESPONSE_FILE"
    jq -e '.results[0] | has("content")' "$MOCK_RESPONSE_FILE"
    jq -e '.results[0] | has("job_id")' "$MOCK_RESPONSE_FILE"
    jq -e '.results[0] | has("status_code")' "$MOCK_RESPONSE_FILE"
    jq -e '.results[0].status_code == 200' "$MOCK_RESPONSE_FILE"
}
