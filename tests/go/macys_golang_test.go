package main

import (
	"bytes"
	"encoding/json"
	"io/ioutil"
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"testing"
)

type MockResponse struct {
	Results []struct {
		Content    string `json:"content"`
		CreatedAt  string `json:"created_at"`
		UpdatedAt  string `json:"updated_at"`
		Page       int    `json:"page"`
		URL        string `json:"url"`
		JobID      string `json:"job_id"`
		StatusCode int    `json:"status_code"`
	} `json:"results"`
}

func loadMockResponse(t *testing.T) MockResponse {
	mockFile := filepath.Join("..", "shared", "mock_responses.json")
	data, err := ioutil.ReadFile(mockFile)
	if err != nil {
		t.Fatalf("Failed to read mock response file: %v", err)
	}

	var mockResponse MockResponse
	err = json.Unmarshal(data, &mockResponse)
	if err != nil {
		t.Fatalf("Failed to unmarshal mock response: %v", err)
	}

	return mockResponse
}

func TestPayloadStructure(t *testing.T) {
	payload := map[string]string{
		"source": "universal",
		"url":    "https://www.macys.com/shop/sale?id=3536&trackingid=407x733169&m_sc=sem&m_sb=google&m_tp=trademark&m_ac=google_trademark_international&m_ag=macy%27score_exact&m_cn=ggl_trademark_intl_lithuania_exact&m_pi=go_cmp-94807774_adg-154238318312_ad-674544461209_kwd-252677959_dev-c_ext-102882313401_prd-&gad_source=1&gclid=cj0kcqiayewrbhddarisagp1mwsg3z6ogoqrztdycjyqio5togc316ldkwuqkkbhrmiv4i_ho0gcjlkaagl6ealw_wcb",
	}

	if payload["source"] != "universal" {
		t.Errorf("Expected source to be 'universal', got %s", payload["source"])
	}

	if payload["url"] == "" {
		t.Error("URL should not be empty")
	}

	if len(payload) != 2 {
		t.Errorf("Expected payload to have 2 fields, got %d", len(payload))
	}
}

func TestAuthenticationSetup(t *testing.T) {
	mockResponse := loadMockResponse(t)
	
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		username, password, ok := r.BasicAuth()
		if !ok {
			t.Error("Basic auth not found in request")
		}
		if username != "test_user" || password != "test_pass" {
			t.Errorf("Expected credentials test_user:test_pass, got %s:%s", username, password)
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(mockResponse)
	}))
	defer server.Close()

	payload := map[string]string{
		"source": "universal",
		"url":    "https://www.macys.com/shop/sale",
	}

	jsonValue, _ := json.Marshal(payload)
	client := &http.Client{}
	request, _ := http.NewRequest("POST", server.URL, bytes.NewBuffer(jsonValue))
	request.SetBasicAuth("test_user", "test_pass")

	response, err := client.Do(request)
	if err != nil {
		t.Fatalf("Request failed: %v", err)
	}
	defer response.Body.Close()

	if response.StatusCode != 200 {
		t.Errorf("Expected status code 200, got %d", response.StatusCode)
	}
}

func TestHTTPClientConfiguration(t *testing.T) {
	mockResponse := loadMockResponse(t)
	
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Method != "POST" {
			t.Errorf("Expected POST method, got %s", r.Method)
		}

		if r.Header.Get("Content-Type") != "application/json" {
			t.Errorf("Expected Content-Type application/json, got %s", r.Header.Get("Content-Type"))
		}

		body, _ := ioutil.ReadAll(r.Body)
		var payload map[string]string
		json.Unmarshal(body, &payload)

		if payload["source"] != "universal" {
			t.Errorf("Expected source 'universal', got %s", payload["source"])
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(mockResponse)
	}))
	defer server.Close()

	payload := map[string]string{
		"source": "universal",
		"url":    "https://www.macys.com/shop/sale",
	}

	jsonValue, _ := json.Marshal(payload)
	client := &http.Client{}
	request, _ := http.NewRequest("POST", server.URL, bytes.NewBuffer(jsonValue))
	request.Header.Set("Content-Type", "application/json")
	request.SetBasicAuth("user", "pass1")

	response, err := client.Do(request)
	if err != nil {
		t.Fatalf("Request failed: %v", err)
	}
	defer response.Body.Close()

	if response.StatusCode != 200 {
		t.Errorf("Expected status code 200, got %d", response.StatusCode)
	}
}

func TestAPIResponseStructure(t *testing.T) {
	mockResponse := loadMockResponse(t)
	
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(mockResponse)
	}))
	defer server.Close()

	payload := map[string]string{
		"source": "universal",
		"url":    "https://www.macys.com/shop/sale",
	}

	jsonValue, _ := json.Marshal(payload)
	client := &http.Client{}
	request, _ := http.NewRequest("POST", server.URL, bytes.NewBuffer(jsonValue))
	request.SetBasicAuth("user", "pass1")

	response, err := client.Do(request)
	if err != nil {
		t.Fatalf("Request failed: %v", err)
	}
	defer response.Body.Close()

	var result MockResponse
	json.NewDecoder(response.Body).Decode(&result)

	if len(result.Results) == 0 {
		t.Error("Expected results array to have at least one item")
	}

	firstResult := result.Results[0]
	if firstResult.Content == "" {
		t.Error("Expected content field to be non-empty")
	}
	if firstResult.JobID == "" {
		t.Error("Expected job_id field to be non-empty")
	}
	if firstResult.StatusCode != 200 {
		t.Errorf("Expected status_code 200, got %d", firstResult.StatusCode)
	}
}
