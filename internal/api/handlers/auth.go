package handlers

import (
	"encoding/json"
	"net/http"
	"strings"
	"time"

	"github.com/golang-jwt/jwt/v5"

	"backup/internal/api/middleware"
	"backup/pkg/database"
	"backup/pkg/models"
)

type AuthHandler struct {
	store    database.UsersStore
	jwtKey   []byte
	issuer   string
	audience string
}

func NewAuthHandler(store database.UsersStore, jwtKey []byte, issuer string, audience string) *AuthHandler {
	return &AuthHandler{
		store:    store,
		jwtKey:   jwtKey,
		issuer:   issuer,
		audience: audience,
	}
}

// UserLogin represents the expected request body.
type UserLogin struct {
	Login    string `json:"login"`
	Password string `json:"password"`
}

// LoginError represents an error response.
type LoginError struct {
	Reason string `json:"reason"`
}

func (h *AuthHandler) createToken(user *models.DBUser) (string, time.Time, error) {
	expires := time.Now().UTC().Add(30 * time.Minute)

	roles := ""
	if len(user.Roles) > 0 {
		roles = strings.Join(user.Roles, ",")
	}

	claims := &middleware.UserClaims{
		Sub:   user.Login,
		Roles: roles,
		RegisteredClaims: jwt.RegisteredClaims{
			Issuer:    h.issuer,
			Audience:  jwt.ClaimStrings{h.audience},
			ExpiresAt: jwt.NewNumericDate(expires),
		},
	}

	token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	tokenStr, err := token.SignedString(h.jwtKey)
	return tokenStr, expires, err
}

// Login handles user authentication.
func (h *AuthHandler) Login(w http.ResponseWriter, r *http.Request) {
	var input UserLogin
	if err := json.NewDecoder(r.Body).Decode(&input); err != nil {
		w.WriteHeader(http.StatusBadRequest)
		json.NewEncoder(w).Encode(LoginError{Reason: "Invalid request payload"})
		return
	}

	if strings.TrimSpace(input.Password) == "" {
		w.WriteHeader(http.StatusBadRequest)
		json.NewEncoder(w).Encode(LoginError{Reason: "Password cannot be empty"})
		return
	}

	user, err := h.store.GetUser(input.Login, input.Password)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	if user == nil {
		http.Error(w, "Unauthorized", http.StatusUnauthorized)
		return
	}

	tokenStr, expires, err := h.createToken(user)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"token":   tokenStr,
		"expires": expires.Format(time.RFC3339),
	})
}

// Refresh generates a new token for an already authenticated user.
func (h *AuthHandler) Refresh(w http.ResponseWriter, r *http.Request) {
	claims, ok := r.Context().Value(middleware.UserClaimsKey).(*middleware.UserClaims)
	if !ok || claims.Sub == "" {
		w.WriteHeader(http.StatusBadRequest)
		json.NewEncoder(w).Encode(LoginError{Reason: "Request not well formated"})
		return
	}

	// Fetch user from DB to get the latest roles
	user, err := h.store.GetUser(claims.Sub, "")
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}
	if user == nil {
		http.Error(w, "Unauthorized", http.StatusUnauthorized)
		return
	}

	tokenStr, expires, err := h.createToken(user)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"token":   tokenStr,
		"expires": expires.Format(time.RFC3339),
	})
}

// Create registers a new user (admin only).
func (h *AuthHandler) Create(w http.ResponseWriter, r *http.Request) {
	var input UserLogin
	if err := json.NewDecoder(r.Body).Decode(&input); err != nil {
		w.WriteHeader(http.StatusBadRequest)
		json.NewEncoder(w).Encode(LoginError{Reason: "Invalid request payload"})
		return
	}

	err := h.store.AddUser(input.Login, input.Password, nil) // Add roles if needed later
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusNoContent)
}
