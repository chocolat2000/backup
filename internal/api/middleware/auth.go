package middleware

import (
	"context"
	"log"
	"net/http"
	"strings"

	"github.com/golang-jwt/jwt/v5"
)

type contextKey string

const (
	UserClaimsKey contextKey = "user_claims"
)

// UserClaims represents the JWT claims used in the application.
type UserClaims struct {
	Sub   string `json:"sub"`
	Roles string `json:"roles"`
	jwt.RegisteredClaims
}

// AuthMiddleware provides JWT authentication and authorization.
type AuthMiddleware struct {
	key      []byte
	issuer   string
	audience string
}

func NewAuthMiddleware(key []byte, issuer string, audience string) *AuthMiddleware {
	return &AuthMiddleware{
		key:      key,
		issuer:   issuer,
		audience: audience,
	}
}

// RequireAuth extracts and validates the JWT from the Authorization header.
func (am *AuthMiddleware) RequireAuth(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		authHeader := r.Header.Get("Authorization")
		if authHeader == "" || !strings.HasPrefix(authHeader, "Bearer ") {
			http.Error(w, "Unauthorized", http.StatusUnauthorized)
			return
		}

		tokenStr := strings.TrimPrefix(authHeader, "Bearer ")

		claims := &UserClaims{}
		token, err := jwt.ParseWithClaims(tokenStr, claims, func(token *jwt.Token) (interface{}, error) {
			return am.key, nil
		}, jwt.WithIssuer(am.issuer), jwt.WithAudience(am.audience), jwt.WithValidMethods([]string{"HS256"}))

		if err != nil || !token.Valid {
			http.Error(w, "Unauthorized", http.StatusUnauthorized)
			return
		}

		// Attach claims to context
		ctx := context.WithValue(r.Context(), UserClaimsKey, claims)
		next.ServeHTTP(w, r.WithContext(ctx))
	})
}

// RequireRole ensures the authenticated user has a specific role.
// It must be chained after RequireAuth.
func (am *AuthMiddleware) RequireRole(requiredRole string, next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		claims, ok := r.Context().Value(UserClaimsKey).(*UserClaims)
		if !ok {
			http.Error(w, "Unauthorized", http.StatusUnauthorized)
			return
		}


		roles := strings.Split(claims.Roles, ",")
		hasRole := false
		for _, role := range roles {
			if strings.TrimSpace(role) == requiredRole {
				hasRole = true
				break
			}
		}

		if !hasRole {
			http.Error(w, "Forbidden", http.StatusForbidden)
			return
		}

		next.ServeHTTP(w, r)
	})
}

// Logging is a simple request logger middleware.
func Logging(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		log.Printf("%s %s", r.Method, r.URL.Path)
		next.ServeHTTP(w, r)
	})
}
